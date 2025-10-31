using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Finly.Helpers;
using Finly.Models;
using Finly.Services;

namespace Finly.ViewModels
{
    public class AccountViewModel : INotifyPropertyChanged
    {
        private readonly int _userId;

        public string Username { get; }
        public string Email { get; }
        public DateTime CreatedAt { get; }

        public ObservableCollection<BankConnectionModel> BankConnections { get; } = new();
        public ObservableCollection<BankAccountModel> BankAccounts { get; } = new();

        public ICommand ConnectBankCommand { get; }
        public ICommand DisconnectBankCommand { get; }
        public ICommand SyncNowCommand { get; }
        public string? FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(); } }
        public string? LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(); } }
        public string? Address { get => _address; set { _address = value; OnPropertyChanged(); } }

        public string? CompanyName { get => _companyName; set { _companyName = value; OnPropertyChanged(); } }
        public string? CompanyNip { get => _companyNip; set { _companyNip = value; OnPropertyChanged(); } }
        public string? CompanyAddress { get => _companyAddress; set { _companyAddress = value; OnPropertyChanged(); } }

        private string? _firstName, _lastName, _address;
        private string? _companyName, _companyNip, _companyAddress;

        public ICommand SaveProfileCommand { get; }


        private string? _pwMsg;
        private Brush? _pwBrush;

        public string? PasswordChangeMessage
        {
            get => _pwMsg;
            set { _pwMsg = value; OnPropertyChanged(); }
        }

        public Brush? PasswordChangeBrush
        {
            get => _pwBrush;
            set { _pwBrush = value; OnPropertyChanged(); }
        }


        public AccountViewModel(int userId)
        {
            _userId = userId;
            Username = UserService.GetUsername(userId);
            Email = UserService.GetEmail(userId);
            CreatedAt = UserService.GetCreatedAt(userId);

            // wczytaj profil
            var p = UserService.GetProfile(userId);
            FirstName = p.FirstName; LastName = p.LastName; Address = p.Address;
            CompanyName = p.CompanyName; CompanyNip = p.CompanyNip; CompanyAddress = p.CompanyAddress;

            ConnectBankCommand = new RelayCommand(_ => ConnectBank());
            DisconnectBankCommand = new RelayCommand(c => { if (c is BankConnectionModel m) { OpenBankingService.Disconnect(m.Id); Load(); } });
            SyncNowCommand = new RelayCommand(_ => { OpenBankingService.SyncNow(_userId); Load(); });

            SaveProfileCommand = new RelayCommand(_ => SaveProfile());

            Load();
        }

        private void SaveProfile()
        {
            var p = new UserProfile
            {
                FirstName = FirstName,
                LastName = LastName,
                Address = Address,
                CompanyName = CompanyName,
                CompanyNip = CompanyNip,
                CompanyAddress = CompanyAddress
            };
            UserService.UpdateProfile(_userId, p);
            ToastService.Success("Zapisano dane profilu.");
        }

        private void Load()
        {
            BankConnections.Clear();
            foreach (var c in OpenBankingService.GetConnections(_userId))
                BankConnections.Add(c);

            BankAccounts.Clear();
            foreach (var a in OpenBankingService.GetAccounts(_userId))
                BankAccounts.Add(a);
        }

        private void ConnectBank()
        {
            if (OpenBankingService.ConnectDemo(_userId))
                Load();
        }

        public void ChangePassword(string oldPwd, string newPwd, string newPwdRepeat)
        {
            if (string.IsNullOrWhiteSpace(newPwd) || newPwd != newPwdRepeat)
            {
                PasswordChangeMessage = "Hasła nie są takie same.";
                PasswordChangeBrush = Brushes.IndianRed;
                return;
            }

            var ok = UserService.ChangePassword(_userId, oldPwd, newPwd);
            PasswordChangeMessage = ok ? "Zmieniono hasło." : "Błędne obecne hasło.";
            PasswordChangeBrush = ok ? Brushes.LimeGreen : Brushes.IndianRed;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
