using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_rab_2_Husainova_R.Z_bpi_23_02.Model
{
    public class ArraySorter
    {
        // Общий счётчик сравнений (разделяемый ресурс)
        private long _totalComparisons;
        private readonly object _locker = new object();
        // Делегаты и события для уведомления о завершении сортировки
        public delegate void SortCompletedHandler(int[] sortedArray, long comparisons, double elapsedMilliseconds);
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
        public void BubbleSort(int[] originalArray)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int n = array.Length;
            int totalOperations = n * (n - 1) / 2; 
            int currentOperation = 0;
            int lastReportedPercent = -1;
            for (int i = 0; i < array.Length - 1; i++)
            {
                for (int j = 0; j < array.Length - 1 - i; j++)
                {
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
            }
            watch.Stop();
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            BubbleSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }
        // Метод для быстрой сортировки (обёртка)
        public void QuickSort(int[] originalArray)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int totalElements = array.Length;
            int[] processedTracker = { 0 };
            QuickSortRecursive(array, 0, array.Length - 1, ref comparisons, processedTracker, totalElements);
            watch.Stop();
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            QuickSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }
        private void QuickSortRecursive(int[] arr, int left, int right, ref long comparisons, int[] processedTracker, int totalElements)
        {
            if (left < right)
            {
                int pivotIndex = Partition(arr, left, right, ref comparisons);
                int processed = processedTracker[0] + (right - left + 1);
                int percent = Math.Min(100, (int)((processed * 100.0) / totalElements));
                QuickSortProgressChanged?.Invoke(percent);
                processedTracker[0] = processed;

                QuickSortRecursive(arr, left, pivotIndex - 1, ref comparisons, processedTracker, totalElements);
                QuickSortRecursive(arr, pivotIndex + 1, right, ref comparisons, processedTracker, totalElements);
            }
        }
        private int Partition(int[] arr, int left, int right, ref long comparisons)
        {
            int pivot = arr[right];
            int i = left - 1;
            for (int j = left; j < right; j++)
            {
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
        public void InsertionSort(int[] originalArray)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int n = array.Length;
            int lastReportedPercent = -1;
            for (int i = 1; i < array.Length; i++)
            {
                int key = array[i];
                int j = i - 1;
                while (j >= 0 && array[j] > key)
                {
                    comparisons++;
                    array[j + 1] = array[j];
                    j--;
                }
                comparisons++; // учёт последнего сравнения, когда условие не выполнено
                array[j + 1] = key;
                int percent = (int)((i * 100.0) / n);
                if (percent != lastReportedPercent)
                {
                    InsertionSortProgressChanged?.Invoke(percent);
                    lastReportedPercent = percent;
                }
            }
            watch.Stop();
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            InsertionSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }
        // Метод шейкерной сортировки
        public void ShakerSort(int[] originalArray)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            int n = array.Length;
            int lastReportedPercent = -1;
            int totalPasses = n;
            int currentPass = 0;     

            int start = 0;
            int end = n - 1;
            bool swapped = true;
            while (swapped)
            {
                swapped = false;

                for (int i = start; i < end; i++)
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
                    comparisons++;
                    if (array[i] < array[i - 1])
                    {
                        int temp = array[i];
                        array[i] = array[i - 1];
                        array[i - 1] = temp;
                        swapped = true;
                    }
                }
                currentPass++;
                percent = Math.Min(100, (int)((currentPass * 100.0) / totalPasses));
                if (percent != lastReportedPercent)
                {
                    ShakerSortProgressChanged?.Invoke(percent);
                    lastReportedPercent = percent;
                }
                start++;
            }

            watch.Stop();
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            ShakerSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }
    }
}