namespace MultiplyMatrix
{
    sealed internal class ClusterData
    {
        public int begin;
        public int end;
    }


    internal class Program
    {
        static void Fill(int[,] matrix)
        {
            Random rnd = new Random();
            for (int i = 0; i < matrix.GetLength(0); i++)
                for (int j = 0; j < matrix.GetLength(1); j++)
                    matrix[i, j] = rnd.Next(0, 100);
        }

        static int[,] MultiplyMatrix(int[,] matrix1, int[,] matrix2)
        {
            int rowA = matrix1.GetLength(0);
            int colA = matrix1.GetLength(1);
            int rowB = matrix2.GetLength(0);
            int colB = matrix2.GetLength(1);
            int temp;
            int[,] result = new int[rowA, colA];
            for (int i = 0; i < rowA; i++)
            {
                for (int j = 0; j < colB; j++)
                {
                    temp = 0; // using temp saves 1/5 time. For n = 2000 from 2500ms to 1900 ish
                    for (int k = 0; k < colA; k++)
                    {
                        temp += matrix1[i, k] * matrix2[k, j];
                    }
                    result[i, j] = temp;
                }
            }
            return result;
        }

        static int[,] MultiplyMatrixTask(int[,] matrix1, int[,] matrix2)
        {
            int rowA = matrix1.GetLength(0);
            int colA = matrix1.GetLength(1);
            int rowB = matrix2.GetLength(0);
            int colB = matrix2.GetLength(1);
            int[,] result = new int[rowA, colA];
            Task[] tasks = new Task[rowA];

            for (int i = 0; i < rowA; i++)
            {
                var multiplication = new Task((parameter) =>
                {
                    int ii = (int)parameter;
                    for (int j = 0; j < colB; j++)
                        for (int k = 0; k < colA; k++)
                            result[ii, j] += matrix1[ii, k] * matrix2[k, j];
                }, i);
                tasks[i] = multiplication;
                multiplication.Start();
            }
            Task.WaitAll(tasks);
            return result;
        }

        static int Lesser(int a, int b)
        {
            return a < b ? a : b; //ISC suggested it
        }

        static int[,] MultiplyMatrixCluster(int[,] matrix1, int[,] matrix2)
        {
            // check if multiplycation is possible (I dont check it, beacause work with square matrices)
            int rA = matrix1.GetLength(0);
            int cA = matrix1.GetLength(1);
            int rB = matrix2.GetLength(0);
            int cB = matrix2.GetLength(1);
            int[,] result = new int[rA, cA];
            int numThreads = Environment.ProcessorCount;
            Task[] tasks = new Task[numThreads];

            int rowsPerThread = rA / numThreads;
            if (rA % numThreads != 0)
                rowsPerThread++;
            int remainingRows = rA;

            int tasks_num = 0;
            //if rows%numThreads != 0 we increase rowsPerThread by 1
            //and with the use of Lesser() func handle last part, which is less then rowsPerThread
            for (int ib = 0, ie = rowsPerThread; ib < rA; ib += Lesser(rowsPerThread, remainingRows),
                ie += Lesser(rowsPerThread, remainingRows - rowsPerThread), remainingRows -= rowsPerThread)
            {
                var multiplication = new Task((object obj) =>
                {
                    //Probably the is a way to not give object to here but i didnt find it.
                    ClusterData customData = obj as ClusterData;
                    int ri = customData.begin;
                    int re = customData.end;
                    for (int i = ri; i < re; i++)
                    {
                        for (int j = 0; j < cB; j++)
                        {
                            for (int k = 0; k < cA; k++)
                            {
                                result[i, j] += matrix1[i, k] * matrix2[k, j];
                            }
                        }
                    }
                }, new ClusterData() { begin = ib, end = ie });
                tasks[tasks_num] = multiplication;
                multiplication.Start();
                tasks_num++;
            }
            Task.WaitAll(tasks);
            return result;
        }

        static void Main(string[] args)
        {
            var watch = new System.Diagnostics.Stopwatch();

            int n = 1000;
            int m = n; //square matrix
            int[,] matrix1 = new int[n, m];
            int[,] matrix2 = new int[n, m];
            int[,] matrix3 = new int[n, m];
            int[,] matrix4 = new int[n, m];
            int[,] matrix5 = new int[n, m];

            Fill(matrix1);
            Fill(matrix2);
            Fill(matrix3);

            watch.Start();
            matrix3 = MultiplyMatrix(matrix1, matrix2);
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds + " Non async");

            watch.Restart();
            matrix4 = MultiplyMatrixTask(matrix1, matrix2);
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds + " Async");

            watch.Restart();
            matrix5 = MultiplyMatrixCluster(matrix2, matrix3);
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds + " Better Async");
        }
    }
}