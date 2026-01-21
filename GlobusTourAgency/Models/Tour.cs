using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Media;

namespace GlobusTourAgency.Models
{
    public class Tour : INotifyPropertyChanged
    {
        private int _freeSeats;
        private decimal _discount;
        private string _photoFileName;

        public int Id { get; set; }
        public int TourCode { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public int DurationDays { get; set; }
        public DateTime StartDate { get; set; }
        public decimal Price { get; set; }
        public string BusType { get; set; }
        public int Capacity { get; set; }

        public int FreeSeats
        {
            get => _freeSeats;
            set
            {
                if (_freeSeats != value)
                {
                    _freeSeats = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsFewSeats));
                    OnPropertyChanged(nameof(FreeSeatsInfo));
                }
            }
        }

        public string PhotoFileName
        {
            get => _photoFileName;
            set
            {
                if (_photoFileName != value)
                {
                    _photoFileName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PhotoPath));
                }
            }
        }

        public decimal Discount
        {
            get => _discount;
            set
            {
                if (_discount != value)
                {
                    _discount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSpecialOffer));
                    OnPropertyChanged(nameof(DiscountInfo));
                }
            }
        }

        public string PhotoPath
        {
            get
            {
                if (string.IsNullOrEmpty(PhotoFileName))
                    return GetDefaultImagePath();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string importDir = Path.Combine(baseDir, "import");

                if (!Directory.Exists(importDir))
                {
                    Console.WriteLine($"Папка import не найдена: {importDir}");
                    return GetDefaultImagePath();
                }

                string dbFileName = PhotoFileName.Trim();

                if (dbFileName.EndsWith("_preg"))
                {
                    dbFileName = dbFileName.Replace("_preg", "");
                }

                string fileNameWithExt = dbFileName + ".png";

                string fullPath = Path.Combine(importDir, fileNameWithExt);

                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"Найдено фото: {fileNameWithExt}");
                    return fullPath;
                }

                string originalPath = Path.Combine(importDir, PhotoFileName + ".png");
                if (File.Exists(originalPath))
                {
                    Console.WriteLine($"Найдено фото (оригинальное имя): {PhotoFileName}.png");
                    return originalPath;
                }

                string[] files = Directory.GetFiles(importDir, "*.png");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                    string searchName = dbFileName.ToLower();

                    if (fileName.Contains(searchName) || searchName.Contains(fileName))
                    {
                        Console.WriteLine($"Найдено по частичному совпадению: {Path.GetFileName(file)}");
                        return file;
                    }
                }

                Console.WriteLine($"Не найдено фото для: {PhotoFileName}");
                return GetDefaultImagePath();
            }
        }

        private string GetDefaultImagePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string placeholderPath = Path.Combine(baseDir, "import", "placeholder.png");

            if (File.Exists(placeholderPath))
            {
                return placeholderPath;
            }

            Console.WriteLine("Файл placeholder.png не найден");
            return null;
        }

        public bool IsSpecialOffer => Discount > 15;
        public bool IsFewSeats => Capacity > 0 && FreeSeats < (Capacity * 0.1m);
        public bool IsStartingSoon => (StartDate - DateTime.Now).TotalDays < 7;

        public string FormattedStartDate => StartDate.ToString("dd.MM.yyyy");
        public string FormattedPrice => Price.ToString("N0") + " руб.";
        public string DiscountInfo => Discount > 0 ? $"-{Discount}%" : "";
        public string FreeSeatsInfo => $"{FreeSeats}/{Capacity} мест";
        public string DaysUntilStart
        {
            get
            {
                var days = (StartDate - DateTime.Now).TotalDays;
                if (days < 0) return "Завершен";
                if (days < 1) return "Сегодня";
                if (days < 2) return "Завтра";
                return $"Через {Math.Ceiling(days)} дней";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}