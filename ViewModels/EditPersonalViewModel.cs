using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Finly.Models;
using Finly.Services;

namespace Finly.ViewModels
{
    public class EditPersonalViewModel : INotifyPropertyChanged
    {
        private readonly int _userId;

        public string? Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        public string? FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(); } }
        public string? LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(); } }
        public string? BirthYear { get => _birthYear; set { _birthYear = value; OnPropertyChanged(); } }
        public string? City { get => _city; set { _city = value; OnPropertyChanged(); } }
        public string? PostalCode { get => _postalCode; set { _postalCode = value; OnPropertyChanged(); } }
        public string? HouseNo { get => _houseNo; set { _houseNo = value; OnPropertyChanged(); } }

        private string? _email, _firstName, _lastName, _birthYear, _city, _postalCode, _houseNo;

        public EditPersonalViewModel(int userId)
        {
            _userId = userId;

            // wczytaj aktualne dane
            Email = UserService.GetEmail(userId);

            var p = UserService.GetProfile(userId);
            FirstName = p.FirstName;
            LastName = p.LastName;
            // Jeżeli masz już te pola w profilu – wczytają się.
            BirthYear = p.BirthYear;
            City = p.City;
            PostalCode = p.PostalCode;
            HouseNo = p.HouseNo;
        }

        public bool Save()
        {
            // prosta walidacja
            if (string.IsNullOrWhiteSpace(Email))
            {
                ToastService.Error("Podaj adres e-mail.");
                return false;
            }

            // zaktualizuj e-mail (jeśli przechowujesz go w oddzielnej kolumnie)
            UserService.UpdateEmail(_userId, Email!);

            // zapisz profil
            var p = new UserProfile
            {
                FirstName = FirstName,
                LastName = LastName,
                BirthYear = BirthYear,
                City = City,
                PostalCode = PostalCode,
                HouseNo = HouseNo
            };

            UserService.UpdateProfile(_userId, p);
            ToastService.Success("Zapisano dane osobowe.");
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
