using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Simulator
{
    class Run_Simulator
    {
        static int rows;
        static int cols;
        static int nThreads;
        static int nOperation;
        static String[] strings;
        static ShareableSpreadSheet sheet;
        static string projectDirectory;

        public Run_Simulator(int r,int c, int threads,int operations)
        {
            rows = r;
            cols = c;
            nThreads = threads;
            nOperation = operations;

            sheet = new ShareableSpreadSheet(rows, cols);
            Console.WriteLine("Generate {0}*{1} spreadsheet. It launches {2} concurrent threads. Each thread performs a sequence of {3} random operations.", rows, cols, nThreads, nOperation);
            String[,] grid = sheet.getGrid();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    grid[i, j] = "testcell" + i + j + ",";
                }

            }
            sheet.setGrid(grid);
            strings = new string[nOperation];
            String to_fill = "sherlock";
            for (int i=0;i<strings.Length;i++)
            {
                strings[i] = to_fill + i.ToString();
            }
            string projectpath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            string to_remove = "Simulator";
            projectDirectory = "";
            for (int j = 0; j < projectpath.Length - to_remove.Length; j++)
            {
                projectDirectory += projectpath[j];
            }
            projectDirectory += "spreadsheet";
            
            Thread[] t = new Thread[nThreads];
            for (int i = 0; i < nThreads; i++)
            {
                t[i] = new Thread(ThreadProc);
                t[i].Name= String.Format("User[{0}]", i + 1);
                t[i].Start();
            }
            
            for(int i=0;i<nThreads; i++)
            {
                t[i].Join();
            }

            //Console.WriteLine("finish, save final file as spreadsheet.dat");
            sheet.save(projectDirectory);

        }

        private static void ThreadProc()
        {
            Random rnd = new Random();
            for (int i = 0; i < nOperation; i++)
            {
                int rand_func = rnd.Next(0, 13);
                int str_num = rnd.Next(0, nOperation - 1);
                int r = rnd.Next(0, rows);
                int c = rnd.Next(0, cols);
                if (rand_func == 0)
                {//getCell
                    Console.WriteLine("{0}:[getCell] string \"{3}\" found in cell[{1},{2}]", Thread.CurrentThread.Name, r, c, sheet.getCell(r, c));
                }
                else if (rand_func==1)
                {//setCell
                    String cell = sheet.getCell(r, c);
                    String to_write = strings[str_num] + r.ToString() + c.ToString()+",";
                    if(sheet.setCell(r, c, to_write))
                     Console.WriteLine("{0}:[setCell] string \"{3}\" replace string \"{4}\" in cell[{1},{2}]", Thread.CurrentThread.Name, r, c, sheet.getCell(r, c), cell);
                    else
                        Console.WriteLine("{0}:[setCell]---Failed--- replace string \"{3}\" in cell[{1},{2}]", Thread.CurrentThread.Name, r, c,  cell);
                }
                else if(rand_func==2)
                {//searchString(String str, ref int row, ref int col)
                    int row =-2;
                    int col=-2;
                    if (sheet.searchString(strings[str_num],ref row,ref col))
                    {
                        Console.WriteLine("{0}:[searchString] string \"{1}\" found in cell[{2},{3}]", Thread.CurrentThread.Name, strings[str_num],row,col);
                    }
                    else if(row==-1 && col ==-1)
                    {
                        Console.WriteLine("{0}:[searchString] cannot search! number of Concurrent Search is Limited and now there is no place for +1 searcher... :(", Thread.CurrentThread.Name);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[searchString] string \"{1}\" not exists in this SpreadSheet", Thread.CurrentThread.Name, strings[str_num]);
                    }
                }
                else if (rand_func == 3)
                {//exchangeRows
                    int r1 = rnd.Next(0, rows);
                    if(sheet.exchangeRows(r,r1))
                    {
                        Console.WriteLine("{0}:[exchangeRows] rows [{1}] and [{2}] exchanged successfully.", Thread.CurrentThread.Name, r, r1);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[exchangeRows] ---Fail--- rows [{1}] and [{2}] not exchanged.", Thread.CurrentThread.Name, r, r1);
                    }
                }
                else if (rand_func == 4)
                {//exchangeCols
                    int cc1 = rnd.Next(0, cols);
                    if (sheet.exchangeCols(c, cc1))
                    {
                        Console.WriteLine("{0}:[exchangeCols] cols [{1}] and [{2}] exchanged successfully.", Thread.CurrentThread.Name, c, cc1);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[exchangeCols] ---Fail--- cols [{1}] and [{2}] not exchanged.", Thread.CurrentThread.Name, c, cc1);
                    }


                }
                else if (rand_func == 5)
                {//searchInRow(int row, String str, ref int col)
                    int col = -2;
                    if (sheet.searchInRow(r,strings[str_num],ref col))
                    {
                        Console.WriteLine("{0}:[searchInRow]string \"{1}\" found in cell[{2},{3}]", Thread.CurrentThread.Name, strings[str_num], r, col);
                    }
                    else if (col == -1)
                    {
                        Console.WriteLine("{0}:[searchInRow]cannot search! number of Concurrent Search is Limited and now there is no place for +1 searcher... :(", Thread.CurrentThread.Name);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[searchInRow]string \"{1}\" not exists in this SpreadSheet", Thread.CurrentThread.Name, strings[str_num]);
                    }
                }
                else if (rand_func == 6)
                {//searchInCol(int col, String str, ref int row)
                    int row = -2;
                    if (sheet.searchInCol(c, strings[str_num], ref row))
                    {
                        Console.WriteLine("{0}:[searchInCol]string \"{1}\" found in cell[{2},{3}]", Thread.CurrentThread.Name, strings[str_num], row, c);
                    }
                    else if (row == -1)
                    {
                        Console.WriteLine("{0}:[searchInCol]cannot search! number of Concurrent Search is Limited and now there is no place for +1 searcher... :(", Thread.CurrentThread.Name);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[searchInCol]string \"{1}\" not exists in this SpreadSheet", Thread.CurrentThread.Name, strings[str_num]);
                    }
                }
                else if (rand_func == 7)
                {//searchInRange(int col1, int col2, int row1, int row2, String str, ref int row, ref int col)
                    int row = -2;
                    int col = -2;
                    int row1 = r;
                    int row2 = rnd.Next(0, rows);
                    int col1 = c;
                    int col2 = rnd.Next(0, cols);
                    if (sheet.searchInRange(Math.Min(col1,col2),Math.Max(col1,col2),Math.Min(row1,row2),Math.Max(row1,row2),strings[str_num],ref row,ref col))
                    {
                        Console.WriteLine("{0}:[searchInRange]string \"{1}\" found in cell[{2},{3}]", Thread.CurrentThread.Name, strings[str_num], row, col);
                    }
                    else if (row == -1 && col == -1)
                    {
                        Console.WriteLine("{0}:[searchInRange]cannot search! number of Concurrent Search is Limited and now there is no place for +1 searcher... :(", Thread.CurrentThread.Name);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[searchInRange]string \"{1}\" not exists in the range [{2}-{3},{4}-{5}]", Thread.CurrentThread.Name, strings[str_num], Math.Min(row1, row2), Math.Max(row1, row2), Math.Min(col1, col2), Math.Max(col1, col2));
                    }

                }
                else if (rand_func == 8)
                {//addRow(int row1)
                    if(sheet.addRow(r))
                    {
                        Interlocked.Increment(ref rows);//rows++;
                        Console.WriteLine("{0}:[addRow] add a new row after row number {1}", Thread.CurrentThread.Name, r);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[addRow] ---Fail--- didnt add a row after {1}", Thread.CurrentThread.Name, r);
                    }
                }
                else if (rand_func == 9)
                {//addCol(int col1)
                    if (sheet.addCol(c))
                    {
                        Interlocked.Increment(ref cols);//cols++;
                        Console.WriteLine("{0}:[addCol] add a new col after col number {1}", Thread.CurrentThread.Name, c);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[addCol] ---Fail--- didnt add a col after {1}", Thread.CurrentThread.Name, c);
                    }
                }
                else if (rand_func == 10)//getSize(ref int nRows, ref int nCols)
                {
                    int row_ref = -1;
                    int col_ref = -1;
                    sheet.getSize(ref row_ref, ref col_ref);
                    if(row_ref ==-1 && col_ref==-1)
                    {
                        Console.WriteLine("{0}:[getSize] ---Fail--- ", Thread.CurrentThread.Name);
                    }
                    else
                    {
                        Console.WriteLine("{0}:[getSize] sheet size = {1}X{2}", Thread.CurrentThread.Name, row_ref,col_ref);
                    }
                }
                else if(rand_func==11)//setConcurrentSearchLimit(int nUsers)
                {
                    int users_limit = rnd.Next(0, nThreads);
                    if (sheet.setConcurrentSearchLimit(users_limit))
                    {
                        Console.WriteLine("{0}:[setConcurrentSearchLimit] set limit to {1}", Thread.CurrentThread.Name, users_limit);
                    }
                    else
                    {
                        Console.WriteLine("{0}:---Fail---[setConcurrentSearchLimit] try to set limit to {1}", Thread.CurrentThread.Name, users_limit);
                    }
                }
                else if(rand_func==12)
                {//save(String fileName)
/*                    string projectpath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
                    string to_remove = "Simulator";
                    string projectDirectory="";
                    for (int j =0;j< projectpath.Length - to_remove.Length; j++)
                    {
                        projectDirectory += projectpath[j];
                    }
                    projectDirectory += "spreadsheet";*/
                    if(sheet.save(projectDirectory))
                    {
                        Console.WriteLine("{0}:[save] save sheet at {1}", Thread.CurrentThread.Name, projectDirectory);
                    }
                    else
                    {
                        Console.WriteLine("{0}:---Fail---[save] try to save sheet at {1}", Thread.CurrentThread.Name, projectDirectory);
                    }
                }
                Thread.Sleep(100);
            }
        }



    }
}
