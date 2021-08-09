using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Simulator
{
    class ShareableSpreadSheet
    {
        //atomic variables
        static long ConcurrentSearchLimit;
        static long search_counter;
        static long non_absolute_counter;
        //sheet lock
        Semaphore resource;
        //searcher counter lock
        Semaphore searcher_counter_save;
        //"reader" - regular user(not an absolute writer) lock
        Semaphore non_absolute_counter_save;
        //maintains an organized entrance - entry lock
        Semaphore Queue;
        //guards for cols and rows
        Mutex[] rows_lock;
        Mutex[] cols_lock;
        //the sheet
        string[,] grid;
        //size of the sheet
        int rows;
        int cols;

        public ShareableSpreadSheet(int nRows, int nCols)
        {
            rows = nRows;
            cols = nCols;
            grid = new string[nRows, nCols];
            Interlocked.Exchange(ref search_counter, 0);
            Interlocked.Exchange(ref non_absolute_counter, 0);
            resource = new Semaphore(0, 1);
            resource.Release();
            searcher_counter_save = new Semaphore(0, 1);
            searcher_counter_save.Release();
            non_absolute_counter_save = new Semaphore(0, 1);
            non_absolute_counter_save.Release();
            Queue = new Semaphore(0, 1);
            Queue.Release();
            rows_lock = new Mutex[nRows + 1];//row guards
            for (int i = 0; i <= nRows; i++)
            {
                rows_lock[i] = new Mutex();
            }
            cols_lock = new Mutex[nCols + 1];//col guards
            for (int i = 0; i <= nCols; i++)
            {
                cols_lock[i] = new Mutex();
            }
            Interlocked.Exchange(ref ConcurrentSearchLimit, -1);
        }
        public String[,] getGrid()
        {
            return grid;
        }

        private void Regular_User_Entry()
        {//reader or non-absolute writer
            Queue.WaitOne();//maintains an organized entrance - one by one and not 2 or more together
            non_absolute_counter_save.WaitOne();//lock this counter - only this current thread have access now
            Interlocked.Increment(ref non_absolute_counter);//non_absolute_counter++;
            if (Interlocked.Read(ref non_absolute_counter) == 1)//first regular user need to lock the sheet from absolute writer
            {
                resource.WaitOne();
            }
            non_absolute_counter_save.Release();
            Queue.Release();//next thread can enter
        }

        private bool Search_Regular_User_Entry()
        {//searcher (reader or non-absolute writer)
            if (Interlocked.Read(ref ConcurrentSearchLimit) == -1)//no limit 
            {
                Regular_User_Entry();
                return true;
            }
            Queue.WaitOne();
            searcher_counter_save.WaitOne();//lock the searcher counter
            if (Interlocked.Read(ref search_counter) == Interlocked.Read(ref ConcurrentSearchLimit))
            {
                //Console.WriteLine("you set a limit ({0}) to the amount of users that search !,How can U forget this limit ?! Shame on you ! now run {1} searcher\n Wait until one of them will finish . . .",ConcurrentSearchLimit,search_counter);
                Queue.Release();
                searcher_counter_save.Release();
                return false; //this thread cant enter now
            }
            non_absolute_counter_save.WaitOne();
            Interlocked.Increment(ref non_absolute_counter);//non_absolute_counter++
            if (Interlocked.Read(ref non_absolute_counter) == 1)
            {
                resource.WaitOne();
            }
            Interlocked.Increment(ref search_counter);//search_counter++;

            searcher_counter_save.Release();
            non_absolute_counter_save.Release();
            Queue.Release();
            return true;
        }

        private void Search_Regular_User_Exit()
        {
            if (Interlocked.Read(ref search_counter) == -1)//no limit...
            {
                Regular_User_Exit();
                return;
            }
            searcher_counter_save.WaitOne();
            non_absolute_counter_save.WaitOne();
            Interlocked.Decrement(ref non_absolute_counter);//non_absolute_counter--;
            Interlocked.Decrement(ref search_counter);//search_counter--;


            if (Interlocked.Read(ref non_absolute_counter) == 0)//last regular user realse and open access to all users
            {
                resource.Release();
            }
            searcher_counter_save.Release();
            non_absolute_counter_save.Release();
        }

        private void Regular_User_Exit()
        {
            non_absolute_counter_save.WaitOne();
            Interlocked.Decrement(ref non_absolute_counter);//non_absolute_counter--;
            if (Interlocked.Read(ref non_absolute_counter) == 0)//last regular user release and open access to all users
            {
                resource.Release();
            }
            non_absolute_counter_save.Release();
        }

        private void absolute_Writer_Entry()
        {
            Queue.WaitOne();
            resource.WaitOne();
            Queue.Release();
        }

        private void absolute_Writer_Exit()
        {
            
            resource.Release();
        }

        public void printGrid()
        {//Regular_User
            Regular_User_Entry();
            //START Critical Section
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write(getCell(i, j));
                }
                Console.WriteLine();
            }
            //FINISH Critical Section
            Regular_User_Exit();
        }
        public void setGrid(string[,] s)
        {
            absolute_Writer_Entry();
            //START Critical Section
            grid = s;
            //END Critical Section
            absolute_Writer_Exit();
        }
        public String getCell(int row, int col)
        {//Regular_User
            // return the string at [row,col]
            String cell;
            Regular_User_Entry();
            //START Critical Section
            if (row>=rows || col>=cols)
            {
                Regular_User_Exit();
                return null;
            }
            rows_lock[row].WaitOne();
            cols_lock[col].WaitOne();
            cell = grid[row, col];
            rows_lock[row].ReleaseMutex();
            cols_lock[col].ReleaseMutex();
            //END Critical Section
            Regular_User_Exit();
            return cell;
        }
        public bool setCell(int row, int col, String str)
        {//writer - non absolute
            if (row > this.rows || col > this.cols || row < 0 || col < 0) { return false; }

            Regular_User_Entry();
            rows_lock[row].WaitOne();
            cols_lock[col].WaitOne();
            //START CS
            grid[row, col] = str;
            //END CS
            rows_lock[row].ReleaseMutex();
            cols_lock[col].ReleaseMutex();
            Regular_User_Exit();

            return true;
        }
        public bool searchString(String str, ref int row, ref int col)
        {//Search_Regular_User_Entry
            // search the cell with string str, and return true/false accordingly.
            // stores the location in row,col.
            // return the first cell that contains the string (search from first row to the last row)
            if (!Search_Regular_User_Entry())
            {
                row = -1;
                col = -1;
                return false;
            }
            bool return_val = false;

            //START Critical Section
            for (int i = 0; i < this.rows && !return_val; i++)
            {
                rows_lock[i].WaitOne();//lock this row
                for (int j = 0; j < this.cols && !return_val; j++)
                {
                    cols_lock[j].WaitOne();//lock this col
                    try
                    {
                        if (grid[i, j].Equals(str))
                        {
                            row = i;
                            col = j;
                            return_val = true; ;
                        }
                    }catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    cols_lock[j].ReleaseMutex();//release this col
                }
                rows_lock[i].ReleaseMutex();//release this row
            }
            //END Critical Section
            Search_Regular_User_Exit();


            return return_val;
        }
        public bool exchangeRows(int row1, int row2)
        {//writer
            // exchange the content of row1 and row2
            if(row1==row2)
            {
                return true;
            }
            absolute_Writer_Entry();
            //START Critical Section
            if (row1<0 || row1>rows || row2<0 || row2>rows)
            {
                absolute_Writer_Exit();
                return false;
            }
            string[] tmp = new string[cols];
            for (int i = 0; i < this.cols; i++)
            {

                tmp[i] = grid[row1, i];
                grid[row1, i] = grid[row2, i];
                grid[row2, i] = tmp[i];

            }
            //END Critical Section
            absolute_Writer_Exit();
            return true;
        }
        public bool exchangeCols(int col1, int col2)
        {//absolute writer
            // exchange the content of col1 and col2
            if (col1 == col2)
            {
                return true;
            }
            if (col1 < 0 || col1 >=cols || col2 < 0 || col2 >= cols)
            {
                return false;
            }
            absolute_Writer_Entry();
            //START Critical Section
            string[] tmp = new string[rows];

            for (int i = 0; i < this.rows; i++)
            {

                tmp[i] = grid[i, col1];
                grid[i, col1] = grid[i, col2];
                grid[i, col2] = tmp[i];

            }
            //END Critical Section
            absolute_Writer_Exit();

            return true;
        }
        public bool searchInRow(int row, String str, ref int col)
        {//Search_Regular_User
         // perform search in specific row

            if (!Search_Regular_User_Entry())
            {
                col = -1;
                return false;
            }
            bool return_val = false;

            //START Critical Section
            for (int i = 0; i < this.cols && !return_val; i++)
            {
                cols_lock[i].WaitOne();
                if (grid[row, i].Equals(str))
                {
                    col = i;
                    return_val = true;
                }
                cols_lock[i].ReleaseMutex();
            }
            //END Critical Section
            Search_Regular_User_Exit();
            return return_val;
        }
        public bool searchInCol(int col, String str, ref int row)
        {//Search_Regular_User
         // perform search in specific col
         //Search_Regular_User_Entry();
            if (!Search_Regular_User_Entry())
            {
                row = -1;
                return false;
            }
            if(col>=cols)
            {
                Search_Regular_User_Exit();
                return false;
            }
            bool return_val = false;
            //START Critical Section
            for (int i = 0; i < this.rows && !return_val; i++)
            {


                rows_lock[i].WaitOne();
                if (i >= rows || col>=cols)
                {
                    rows_lock[i].ReleaseMutex();
                    break;
                }
                if (grid[i, col].Equals(str))
                {
                    row = i;
                    return_val = true;
                }
                rows_lock[i].ReleaseMutex();

            }
            //END Critical Section
            Search_Regular_User_Exit();


            return return_val;
        }
        public bool searchInRange(int col1, int col2, int row1, int row2, String str, ref int row, ref int col)
        {//Search_Regular_User
            // perform search within spesific range: [row1:row2,col1:col2] 
            //includes col1,col2,row1,row2
            //Search_Regular_User_Entry();
            if (row2>row1 || col2>col1 || col1>cols ||col2>cols || row1>rows || row2>rows)
            {
                return false;
            }
            if (!Search_Regular_User_Entry())
            {
                row = -1;
                col = -1;
                return false;
            }
            bool return_val = false;

            for (int i = row1; i < row2 && !return_val; i++)
            {
                for (int j = col1; j < col2 && !return_val; j++)
                {
                    cols_lock[j].WaitOne();
                    if (grid[i, j].Equals(str))
                    {
                        
                        rows_lock[i].WaitOne();
                        if (grid[i, j].Equals(str))
                        {
                            row = i;
                            col = j;
                            rows_lock[i].ReleaseMutex();
                            return_val = true;
                        }
                    }
                    cols_lock[j].ReleaseMutex();
                }
            }

            Search_Regular_User_Exit();

            return return_val; ;
        }
        public bool addRow(int row1)
        {//absolute writer -  
            //add a row after row1
            if(row1>rows)
            {
                return false;
            }
            absolute_Writer_Entry();
            //START Critical Section
            string[,] updatedGrid = new string[rows + 1, cols];
            Mutex[] rows_lock_s = new Mutex[rows + 1];
            for (int i = 0; i <= row1; i++)//1,2
            {
                rows_lock_s[i] = rows_lock[i];
                //rows_lock[i].WaitOne();
                for (int j = 0; j < this.cols; j++)
                {
                    //cols_lock[j].WaitOne();
                    updatedGrid[i, j] = grid[i, j];//getCell(i, j);
                    //cols_lock[j].ReleaseMutex();
                }
                //rows_lock[i].WaitOne();
            }

            rows_lock_s[row1 + 1] = new Mutex();
            for (int i = 0; i < this.cols; i++)// 3
            {
                updatedGrid[row1+1, i] = ("New Row" + rows + i + ",");
            }
            this.rows++;

            for (int i = row1+1; i < this.rows - 1; i++)
            {
                rows_lock_s[i + 1] = rows_lock[i];
                for (int j = 0; j < this.cols; j++)
                {
                    updatedGrid[i + 1, j] = grid[i, j];//getCell(i, j);
                }
            }
            rows_lock = rows_lock_s;
            //setGrid
            this.grid = updatedGrid;
            //END Critical Section
            absolute_Writer_Exit();

            return true;
        }
        public bool addCol(int col1)
        {//absolute writer
            //add a column after col1
            if(col1>cols)
            {
                return false;
            }
            absolute_Writer_Entry();
            //START Critical Section
            string[,] updatedGrid = new String[rows, cols + 1];
            Mutex[] cols_lock_s = new Mutex[this.cols + 1];
            for (int i = 0; i < this.rows; i++)
            {
                for (int j = 0; j <= col1; j++)
                {
                    cols_lock_s[j] = cols_lock[j];
                    updatedGrid[i, j] = grid[i, j];//getCell(i, j);
                }
            }

            cols_lock_s[col1 + 1] = new Mutex();
            for (int i = 0; i < this.rows; i++)
            {
                updatedGrid[i, col1] = ("NewCell" + i + ",");
            }
            this.cols++;

            for (int i = 0; i < this.rows; i++)
            {
                for (int j = col1; j < this.cols - 1; j++)
                {
                    cols_lock_s[j + 1] = cols_lock[j];
                    updatedGrid[i, j + 1] = grid[i, j];//getCell(i, j);
                }
            }
            //update the cols_lock array
            cols_lock = cols_lock_s;
            //setGrid
            this.grid = updatedGrid;
            //END Critical Section
            absolute_Writer_Exit();
            return true;
        }
        public void getSize(ref int nRows, ref int nCols)
        {//reader
            // return the size of the spreadsheet in nRows, nCols
            Regular_User_Entry();
            nRows = this.rows;
            nCols = this.cols;
            Regular_User_Exit();

        }
        public bool setConcurrentSearchLimit(int nUsers)
        {
            // this function aims to limit the number of users that can perform the search operations concurrently.
            // The default is no limit. When the function is called, the max number of concurrent search operations is set to nUsers. 
            // In this case additional search operations will wait for existing search to finish.
            if (nUsers < 0)
            {
                //Console.WriteLine("Not funny :(, limit must be non-negative ! ! !\n for now, there is no limit . . . you can try again with non-negative limit");
                return false;
            }
            absolute_Writer_Entry();
            Interlocked.Exchange(ref ConcurrentSearchLimit, nUsers);//ConcurrentSearchLimit = nUsers;
            absolute_Writer_Exit();
            return true;
        }
        public bool save(String fileName)
        {//absolute writer
            // save the spreadsheet to a file fileName.
            // you can decide the format you save the data. There are several options.
            bool return_val = false;
            absolute_Writer_Entry();
            try
            {
                fileName += ".dat";
                using (StreamWriter writetext = new StreamWriter(fileName))
                {
                    writetext.WriteLine(this.rows);
                    writetext.WriteLine(this.cols);
                    for (int i = 0; i < this.rows; i++)
                    {
                        for (int j = 0; j < this.cols; j++)
                        {
                            string tmpCell = grid[i, j];
                            writetext.Write(tmpCell);
                            if (j != this.cols - 1)
                            {
                                writetext.Write(" , ");
                            }
                        }
                        writetext.WriteLine();
                    }
                    return_val = true;

                }
            }
            catch (Exception e)
            {
                return_val = false;
            }
            absolute_Writer_Exit();
            return return_val;
        }
        public bool load(String fileName)
        {//absolute writer
            // load the spreadsheet from fileName
            // replace the data and size of the current spreadsheet with the loaded data
            ShareableSpreadSheet loadedSpreadSheet;
            bool return_val = false;
            string readText = File.ReadAllText(fileName);
            if (readText == null)
            {
                return_val = false;
            }
            else
            {
                absolute_Writer_Entry();
                string[] data = readText.Split("\r\n");
                int rows = int.Parse(data[0]);
                int cols = int.Parse(data[1]);

                loadedSpreadSheet = new ShareableSpreadSheet(rows, cols);

                for (int i = 2; i < data.Length - 1; i++)
                {
                    string curRow = data[i];
                    string[] tmp = curRow.Split(',');
                    for (int j = 0; j < tmp.Length; j++)
                    {
                        Console.WriteLine(tmp[j]);
                        loadedSpreadSheet.grid[i - 2, j] = tmp[j];
                    }
                }
                return_val = true;
                absolute_Writer_Exit();
                //loadedSpreadSheet.printGrid();

            }
            return return_val;
        }
    }
}
