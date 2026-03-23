using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lab_rab_2_Husainova_R.Z_bpi_23_02.Model
{
    public class ArraySorter
    {
        // Общий счётчик сравнений (разделяемый ресурс)
        private long _totalComparisons;
        private readonly object _locker = new object();
        public bool UseSharedArray { get; set; } = false;
        // для синхронизации доступа к общему массиву 
        private readonly object _arrayAccessLock = new object();
        // Делегаты и события для уведомления о завершении сортировки
        public delegate void SortCompletedHandler(int[] sortedArray, long comparisons, double elapsedMilliseconds, bool wasCancelled);
        public event SortCompletedHandler BubbleSortCompleted;
        public event SortCompletedHandler QuickSortCompleted;
        public event SortCompletedHandler InsertionSortCompleted;
        public event SortCompletedHandler ShakerSortCompleted;
        // Делегаты и события для обновления прогресса
        public delegate void ProgressChangedHandler(int percent);
        public event ProgressChangedHandler BubbleSortProgressChanged;
        public event ProgressChangedHandler QuickSortProgressChanged;
        public event ProgressChangedHandler InsertionSortProgressChanged;
        public event ProgressChangedHandler ShakerSortProgressChanged;
        // Свойство для доступа к общему счётчику
        public long TotalComparisons => _totalComparisons;
        public int MaxDegreeOfParallelism { get; set; } = 1;
        private const int SequentialThreshold = 1000;
        // Генерация случайного массива заданного размера
        public int[] GenerateRandomArray(int size)
        {
            Random rand = new Random();
            int[] array = new int[size];
            for (int i = 0; i < size; i++)
                array[i] = rand.Next(1000); // числа от 0 до 999
            return array;
        }
        // Копирование массива (чтобы каждый поток работал со своей копией)
        private int[] CopyArray(int[] source)
        {
            int[] copy = new int[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
        private bool CompareAndSwap(int[] array, int i, int j, ref long comparisons, CancellationToken cancellationToken)
        {
            lock (_arrayAccessLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                comparisons++;

                if (array[i] > array[j])
                {
                    int temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                    return true;
                }
                return false;
            }
        }
        private int SafeRead(int[] array, int index)
        {
            lock (_arrayAccessLock)
            {
                return array[index];
            }
        }
        // Метод для пузырьковой сортировки (запускается в потоке)
        public void BubbleSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int n = array.Length;
            long totalOperations = (long)n * (n - 1) / 2;  
            long currentOperation = 0;
            int lastReportedPercent = -1;
            bool wasCancelled = false;
            try
            {
                for (int i = 0; i < array.Length - 1; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        wasCancelled = true;
                        break;
                    }

                    for (int j = 0; j < array.Length - 1 - i; j++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }

                        // Если общий массив — используем синхронизированный доступ
                        if (UseSharedArray)
                        {
                            CompareAndSwap(array, j, j + 1, ref comparisons, cancellationToken);
                        }
                        else
                        {
                            comparisons++;
                            if (array[j] > array[j + 1])
                            {
                                int temp = array[j];
                                array[j] = array[j + 1];
                                array[j + 1] = temp;
                            }
                        }

                        currentOperation++;
                        int percent = (int)((currentOperation * 100.0) / totalOperations);

                        // Обновляем прогресс не чаще 1% для производительности
                        if (percent != lastReportedPercent)
                        {
                            BubbleSortProgressChanged?.Invoke(percent);
                            lastReportedPercent = percent;
                        }
                    }
                    if (wasCancelled) break;
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }

            watch.Stop();

            // Синхронизация общего счётчика сравнений
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }

            // Возвращаем копию результата, чтобы UI не держал ссылку на общий массив
            int[] resultSnapshot = (int[])array.Clone();
            BubbleSortCompleted?.Invoke(resultSnapshot, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }
        // Метод для быстрой сортировки (обёртка)
        public void QuickSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            bool wasCancelled = false;

            try
            {
                QuickSortRecursive(array, 0, array.Length - 1, ref comparisons, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }

            watch.Stop();
            if (!wasCancelled)
            {
                lock (_locker)
                {
                    _totalComparisons += comparisons;
                }
            }

            int[] resultSnapshot = (int[])array.Clone();
            QuickSortCompleted?.Invoke(resultSnapshot, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }

        private void QuickSortRecursive(int[] arr, int left, int right,
                                        ref long comparisons, CancellationToken cancellationToken)
        {
            // Проверка отмены
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            if (left >= right)
                return;
            int pivotIndex = Partition(arr, left, right, ref comparisons, cancellationToken);

            QuickSortRecursive(arr, left, pivotIndex - 1, ref comparisons, cancellationToken);
            QuickSortRecursive(arr, pivotIndex + 1, right, ref comparisons, cancellationToken);
        }

        private int Partition(int[] arr, int left, int right,
                              ref long comparisons, CancellationToken cancellationToken)
        {
            int pivot = arr[right];  
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                // Проверка отмены
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();

                comparisons++;

                if (arr[j] < pivot)
                {
                    i++;
                    int temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }

            int temp1 = arr[i + 1];
            arr[i + 1] = arr[right];
            arr[right] = temp1;

            return i + 1;
        }


        // Метод для сортировки вставками
        public void InsertionSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);

            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int n = array.Length;
            int lastReportedPercent = -1;
            bool wasCancelled = false;

            try
            {
                for (int i = 1; i < array.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        wasCancelled = true;
                        break;
                    }

                    int key = UseSharedArray ? SafeRead(array, i) : array[i];
                    int j = i - 1;

                    while (j >= 0)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }

                        int currentVal = UseSharedArray ? SafeRead(array, j) : array[j];

                        if (currentVal > key)
                        {
                            comparisons++;
                            if (UseSharedArray)
                            {
                                lock (_arrayAccessLock)
                                {
                                    array[j + 1] = array[j];
                                }
                            }
                            else
                            {
                                array[j + 1] = array[j];
                            }
                            j--;
                        }
                        else
                        {
                            comparisons++;
                            break;
                        }
                    }

                    if (wasCancelled) break;

                    if (UseSharedArray)
                    {
                        lock (_arrayAccessLock)
                        {
                            array[j + 1] = key;
                        }
                    }
                    else
                    {
                        array[j + 1] = key;
                    }

                    int percent = (int)((i * 100.0) / n);
                    if (percent != lastReportedPercent)
                    {
                        InsertionSortProgressChanged?.Invoke(percent);
                        lastReportedPercent = percent;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }

            watch.Stop();

            if (!wasCancelled)
            {
                lock (_locker)
                {
                    _totalComparisons += comparisons;
                }
            }

            int[] resultSnapshot = (int[])array.Clone();
            InsertionSortCompleted?.Invoke(resultSnapshot, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }
        // Метод шейкерной сортировки
        public void ShakerSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);

            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            int n = array.Length;
            int lastReportedPercent = -1;
            long totalPasses = n;
            int currentPass = 0;
            bool wasCancelled = false;
            int start = 0;
            int end = n - 1;
            bool swapped = true;

            try
            {
                while (swapped)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        wasCancelled = true;
                        break;
                    }

                    swapped = false;

                    // Прямой проход
                    for (int i = start; i < end; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }

                        if (UseSharedArray)
                        {
                            CompareAndSwap(array, i, i + 1, ref comparisons, cancellationToken);
                            // Проверяем, был ли обмен (упрощённо — считаем, что если сравнивали, то возможен обмен)
                            swapped = true;
                        }
                        else
                        {
                            comparisons++;
                            if (array[i] > array[i + 1])
                            {
                                int temp = array[i];
                                array[i] = array[i + 1];
                                array[i + 1] = temp;
                                swapped = true;
                            }
                        }
                    }

                    if (wasCancelled) break;
                    if (!swapped) break;

                    currentPass++;
                    int percent = Math.Min(100, (int)((currentPass * 100.0) / totalPasses));
                    if (percent != lastReportedPercent)
                    {
                        ShakerSortProgressChanged?.Invoke(percent);
                        lastReportedPercent = percent;
                    }

                    swapped = false;
                    end--;

                    // Обратный проход
                    for (int i = end; i > start; i--)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }

                        if (UseSharedArray)
                        {
                            CompareAndSwap(array, i - 1, i, ref comparisons, cancellationToken);
                            swapped = true;
                        }
                        else
                        {
                            comparisons++;
                            if (array[i] < array[i - 1])
                            {
                                int temp = array[i];
                                array[i] = array[i - 1];
                                array[i - 1] = temp;
                                swapped = true;
                            }
                        }
                    }

                    if (wasCancelled) break;

                    currentPass++;
                    percent = Math.Min(100, (int)((currentPass * 100.0) / totalPasses));
                    if (percent != lastReportedPercent)
                    {
                        ShakerSortProgressChanged?.Invoke(percent);
                        lastReportedPercent = percent;
                    }
                    start++;
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }

            watch.Stop();

            if (!wasCancelled)
            {
                lock (_locker)
                {
                    _totalComparisons += comparisons;
                }
            }

            int[] resultSnapshot = (int[])array.Clone();
            ShakerSortCompleted?.Invoke(resultSnapshot, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }
    }
}