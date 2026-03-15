using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab_rab_2_Husainova_R.Z_bpi_23_02.Model;
using System;
using System.Threading;
using System.Windows;
namespace Lab_rab_2_Husainova_R.Z_bpi_23_02
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ArraySorter _sorter;
        private readonly SynchronizationContext _uiContext;
        private int[] _originalArray;
        // Наблюдаемые свойства
        [ObservableProperty]
        private int _arraySize = 1000;
        [ObservableProperty]
        private string _originalArrayString;
        [ObservableProperty]
        private string _bubbleSortResult;
        [ObservableProperty]
        private string _quickSortResult;
        [ObservableProperty]
        private string _insertionSortResult;
        [ObservableProperty]
        private string _shakerSortResult;
        [ObservableProperty]
        private string _totalComparisons = "Общее число сравнений: 0";
        [ObservableProperty]
        private bool _canGenerate = true;
        [ObservableProperty]
        private double _bubbleSortProgress;

        [ObservableProperty]
        private double _quickSortProgress;

        [ObservableProperty]
        private double _insertionSortProgress;

        [ObservableProperty]
        private double _shakerSortProgress;

        [ObservableProperty]
        private string _bubbleSortProgressText = "0%";

        [ObservableProperty]
        private string _quickSortProgressText = "0%";

        [ObservableProperty]
        private string _insertionSortProgressText = "0%";

        [ObservableProperty]
        private string _shakerSortProgressText = "0%";
        public MainViewModel()
        {
            _sorter = new ArraySorter();
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
            // Подписка на события завершения сортировки
            _sorter.BubbleSortCompleted += OnBubbleSortCompleted;
            _sorter.QuickSortCompleted += OnQuickSortCompleted;
            _sorter.InsertionSortCompleted += OnInsertionSortCompleted;
            _sorter.ShakerSortCompleted += OnShakerSortCompleted;

            // Подписка на события прогресса
            _sorter.BubbleSortProgressChanged += OnBubbleSortProgressChanged;
            _sorter.QuickSortProgressChanged += OnQuickSortProgressChanged;
            _sorter.InsertionSortProgressChanged += OnInsertionSortProgressChanged;
            _sorter.ShakerSortProgressChanged += OnShakerSortProgressChanged;
        }
        // Обработчики прогресса (вызываются из фоновых потоков)
        private void OnBubbleSortProgressChanged(int percent)
        {
            _uiContext.Post(_ =>
            {
                BubbleSortProgress = percent;
                BubbleSortProgressText = $"{percent}%";
            }, null);
        }

        private void OnQuickSortProgressChanged(int percent)
        {
            _uiContext.Post(_ =>
            {
                QuickSortProgress = percent;
                QuickSortProgressText = $"{percent}%";
            }, null);
        }
        private void OnInsertionSortProgressChanged(int percent)
        {
            _uiContext.Post(_ =>
            {
                InsertionSortProgress = percent;
                InsertionSortProgressText = $"{percent}%";
            }, null);
        }

        private void OnShakerSortProgressChanged(int percent)
        {
            _uiContext.Post(_ =>
            {
                ShakerSortProgress = percent;
                ShakerSortProgressText = $"{percent}%";
            }, null);
        }
        // Команда генерации массива
        [RelayCommand(CanExecute = nameof(CanGenerateArray))]
        private void GenerateArray()
        {
            _originalArray = _sorter.GenerateRandomArray(ArraySize);
            // Отображаем первые 20 элементов
            OriginalArrayString = "Исходный массив: " + string.Join(", ", _originalArray, 0, Math.Min(20,
           _originalArray.Length)) + (ArraySize > 20 ? "..." : "");
            // Сбрасываем предыдущие результаты
            BubbleSortResult = QuickSortResult = InsertionSortResult = ShakerSortResult = null;
            TotalComparisons = "Общее число сравнений: 0";
            BubbleSortProgress = QuickSortProgress = InsertionSortProgress = ShakerSortProgress = 0;
            BubbleSortProgressText = QuickSortProgressText = InsertionSortProgressText = ShakerSortProgressText = "0%";
            // Обновляем состояние команд сортировок
            BubbleSortCommand.NotifyCanExecuteChanged();
            QuickSortCommand.NotifyCanExecuteChanged();
            InsertionSortCommand.NotifyCanExecuteChanged();
            ShakerSortCommand.NotifyCanExecuteChanged();
        }
        private bool CanGenerateArray() => CanGenerate;
        // Пузырьковая сортировка
        private bool CanSortBubble() => _originalArray != null && BubbleSortResult != "Сортируется...";
        [RelayCommand(CanExecute = nameof(CanSortBubble))]
        private void BubbleSort()
        {
            BubbleSortResult = "Сортируется...";
            BubbleSortProgress = 0;
            BubbleSortProgressText = "0%";
            BubbleSortCommand.NotifyCanExecuteChanged();
            Thread thread = new Thread(() => _sorter.BubbleSort(_originalArray));
            thread.Start();
        }
        // Быстрая сортировка
        private bool CanSortQuick() => _originalArray != null && QuickSortResult != "Сортируется...";
        [RelayCommand(CanExecute = nameof(CanSortQuick))]
        private void QuickSort()
        {
            QuickSortResult = "Сортируется...";
            QuickSortCommand.NotifyCanExecuteChanged();
            QuickSortProgress = 0;
            QuickSortProgressText = "0%";
            Thread thread = new Thread(() => _sorter.QuickSort(_originalArray));
            thread.Start();
        }
        // Сортировка вставками
        private bool CanSortInsertion() => _originalArray != null && InsertionSortResult != "Сортируется...";
        [RelayCommand(CanExecute = nameof(CanSortInsertion))]
        private void InsertionSort()
        {
            InsertionSortResult = "Сортируется...";
            InsertionSortProgress = 0;
            InsertionSortProgressText = "0%";
            InsertionSortCommand.NotifyCanExecuteChanged();
            Thread thread = new Thread(() => _sorter.InsertionSort(_originalArray));
            thread.Start();
        }
        // Шейкерная сортировка 
        private bool CanSortShaker() => _originalArray != null && ShakerSortResult != "Сортируется...";
        [RelayCommand(CanExecute = nameof(CanSortShaker))]
        private void ShakerSort()
        {
            ShakerSortResult = "Сортируется...";
            ShakerSortProgress = 0;
            ShakerSortProgressText = "0%";
            ShakerSortCommand.NotifyCanExecuteChanged();

            Thread thread = new Thread(() => _sorter.ShakerSort(_originalArray));
            thread.Start();
        }

        // Обработчики событий (вызываются из фоновых потоков)
        private void OnBubbleSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            _uiContext.Post(_ =>
            {
                BubbleSortResult = $"Пузырьковая: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс,сравнений: { comparisons}";
                BubbleSortProgress = 100;
                BubbleSortProgressText = "100%";
                UpdateTotalComparisons();
                BubbleSortCommand.NotifyCanExecuteChanged();
            }, null);
        }
        private void OnQuickSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            _uiContext.Post(_ =>
            {
                QuickSortResult = $"Быстрая: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс,сравнений: { comparisons}";
                QuickSortProgress = 100;
                QuickSortProgressText = "100%";
                UpdateTotalComparisons();
                QuickSortCommand.NotifyCanExecuteChanged();
            }, null);
        }
        private void OnInsertionSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            _uiContext.Post(_ =>
            {
                InsertionSortResult = $"Вставками: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс, сравнений:{ comparisons}";
                InsertionSortProgress = 100;
                InsertionSortProgressText = "100%";
                UpdateTotalComparisons();
                InsertionSortCommand.NotifyCanExecuteChanged();
            }, null);
        }
        private void OnShakerSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            _uiContext.Post(_ =>
            {
                ShakerSortResult = $"Шейкерная: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс, сравнений: {comparisons}";
                ShakerSortProgress = 100;
                ShakerSortProgressText = "100%";
                UpdateTotalComparisons();
                ShakerSortCommand.NotifyCanExecuteChanged();
            }, null);
        }
        private void UpdateTotalComparisons()
        {
            TotalComparisons = $"Общее число сравнений: {_sorter.TotalComparisons}";
        }
        private string FormatArray(int[] arr)
        {
            if (arr.Length <= 10)
                return string.Join(", ", arr);
            else
                return string.Join(", ", arr, 0, 5) + "...";
        }
    }
}
