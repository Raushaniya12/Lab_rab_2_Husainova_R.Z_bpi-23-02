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
        // Метод для пузырьковой сортировки (запускается в потоке)
        public void BubbleSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = CopyArray(originalArray);
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
                    cancellationToken.ThrowIfCancellationRequested();

                    for (int j = 0; j < array.Length - 1 - i; j++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }

                        comparisons++;
                        currentOperation++;

                        if (array[j] > array[j + 1])
                        {
                            int temp = array[j];
                            array[j] = array[j + 1];
                            array[j + 1] = temp;
                        }
                        int percent = (int)((currentOperation * 100.0) / totalOperations);
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
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            BubbleSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }
        // Метод для быстрой сортировки (обёртка)
        public void QuickSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int totalElements = array.Length;
            bool wasCancelled = false;
            int[] processedTracker = { 0 };
            object progressLock = new object();
            try
            {
                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism
                };

                QuickSortRecursiveParallel(array, 0, array.Length - 1, ref comparisons,
                    processedTracker, totalElements, parallelOptions, progressLock);
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Any(e => e is OperationCanceledException))
                {
                    wasCancelled = true;

                }
            }

                watch.Stop();

            if (!wasCancelled)
            {
                lock (_locker)
                {
                    _totalComparisons += comparisons;
                }
            }

            QuickSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }
        private void QuickSortRecursiveParallel(int[] arr, int left, int right, ref long comparisons,
    int[] processedTracker, int totalElements, ParallelOptions parallelOptions, object progressLock)
        {
            parallelOptions.CancellationToken.ThrowIfCancellationRequested();
            if (left >= right)
            {
                lock (progressLock)
                {
                    int processed = right - left + 1;
                    processedTracker[0] += processed;
                    int percent = Math.Min(100, (int)((processedTracker[0] * 100.0) / totalElements));
                    QuickSortProgressChanged?.Invoke(percent);
                }

                return;
            }

            if (right - left < SequentialThreshold || parallelOptions.MaxDegreeOfParallelism <= 1)
            {
                QuickSortRecursiveSequential(arr, left, right, ref comparisons,
                    processedTracker, totalElements, parallelOptions.CancellationToken, progressLock);
                return;
            }
            long leftComparisons = 0;
            long rightComparisons = 0;
            int pivotIndex = Partition(arr, left, right, ref comparisons, parallelOptions.CancellationToken);

            Parallel.Invoke(parallelOptions,
                () => QuickSortRecursiveParallel(arr, left, pivotIndex - 1, ref leftComparisons,
                    processedTracker, totalElements, parallelOptions, progressLock),
                () => QuickSortRecursiveParallel(arr, pivotIndex + 1, right, ref rightComparisons,
                    processedTracker, totalElements, parallelOptions, progressLock)
            );

            lock (_locker)
            {
                comparisons += leftComparisons + rightComparisons;
            }
        }

        private void QuickSortRecursiveSequential(int[] arr, int left, int right, ref long comparisons,
            int[] processedTracker, int totalElements, CancellationToken cancellationToken, object progressLock)
        {
            if (left >= right) return;

            cancellationToken.ThrowIfCancellationRequested();

            int pivotIndex = Partition(arr, left, right, ref comparisons, cancellationToken);

            lock (progressLock)
            {
                int processed = processedTracker[0] + (right - left + 1);
                int percent = Math.Min(100, totalElements > 0 ? (int)((processed * 100.0) / totalElements) : 100);
                QuickSortProgressChanged?.Invoke(percent);
                processedTracker[0] = processed;
            }

            QuickSortRecursiveSequential(arr, left, pivotIndex - 1, ref comparisons,
                processedTracker, totalElements, cancellationToken, progressLock);
            QuickSortRecursiveSequential(arr, pivotIndex + 1, right, ref comparisons,
                processedTracker, totalElements, cancellationToken, progressLock);
        }

        private int Partition(int[] arr, int left, int right, ref long comparisons, CancellationToken cancellationToken)
        {
            int pivot = arr[right];
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                comparisons++;

                if (arr[j] < pivot)
                {
                    i++;
                    int swaptemp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = swaptemp;
                }
            }

            int temp = arr[i + 1];
            arr[i + 1] = arr[right];
            arr[right] = temp;

            return i + 1;
        }

        // Метод для сортировки вставками
        public void InsertionSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int n = array.Length;
            int lastReportedPercent = -1;
            bool wasCancelled = false;
            try
            {
                for (int i = 1; i < array.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int key = array[i];
                    int j = i - 1;

                    while (j >= 0 && array[j] > key)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }

                        comparisons++;
                        array[j + 1] = array[j];
                        j--;
                    }
                    if (wasCancelled) break;

                    comparisons++;
                    array[j + 1] = key;

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
            InsertionSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }
        // Метод шейкерной сортировки
        public void ShakerSort(int[] originalArray, CancellationToken cancellationToken)
        {
            int[] array = CopyArray(originalArray);
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
                    cancellationToken.ThrowIfCancellationRequested();

                    swapped = false;

                    for (int i = start; i < end; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }

                        comparisons++;
                        if (array[i] > array[i + 1])
                        {
                            int temp = array[i];
                            array[i] = array[i + 1];
                            array[i + 1] = temp;
                            swapped = true;
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

                    for (int i = end; i > start; i--)
                    {
                        // Проверяем отмену
                        if (cancellationToken.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            break;
                        }
                        comparisons++;
                        if (array[i] < array[i - 1])
                        {
                            int temp = array[i];
                            array[i] = array[i - 1];
                            array[i - 1] = temp;
                            swapped = true;
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

            ShakerSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds, wasCancelled);
        }
    }
}