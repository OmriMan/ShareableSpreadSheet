using System;
using System.Threading;

namespace Simulator
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Not funny... Please do as follow : Simulator <rows> <cols> <nThreads> <nOperations>");
                return;
            }
            int rows = int.Parse(args[0]);
            int cols = int.Parse(args[1]);
            int nThreads = int.Parse(args[2]);
            int nOperation = int.Parse(args[3]);
            try
            {
                Run_Simulator run = new Run_Simulator(rows, cols, nThreads, nOperation);
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }


    }
}
