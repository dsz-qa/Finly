using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Finly.Helpers;
using Finly.Models;          // <-- ważne: dla enum AccountType
using Finly.Services;

namespace Finly.ViewModels
{
    public class AccountViewModel : INotifyPropertyChanged
    {
        private readonly int _userId;

        // Podstawowe informacje (readonly w UI)
        public string Username { get; }
        public string Email { get; }
        public DateTime CreatedAt { get; }

        // Typ konta
        public AccountType AccountType { get; }
        public bool IsBusiness => AccountType == AccountType.Business;
        public bool IsPersonal => AccountType == AccountType.Personal;

        // „Bezpieczniki” na puste sekcje
        public bool ShowPersonalSection =>
            IsPersonal && (!string.IsNullOrWhiteSpace(FirstName)
                        || !string.IsNullOrWhiteSpace(LastName)
                        || !string.IsNullOrWhiteSpace(Address));

        public bool ShowCompanySection =>
            IsBusiness && (!string.IsNullOrWhiteSpace(CompanyName)
                        || !string.IsNullOrWhiteSpace(CompanyNip)
                        || !string.IsNullOrWhiteSpace(CompanyAddress));

        // Banki/rachunki
        public ObservableCollection<BankConnectionModel> BankConnections { get; } = new();
        public ObservableCollection<BankAccountModel> BankAccounts { get; } = new();

        public ICommand ConnectBankCommand { get; }
        public ICommand DisconnectBankCommand { get; }
        public ICommand SyncNowCommand { get; }

        // Dane osobowe (readonly w UI)
        public string? FirstName { get => _firstName; private set { _firstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowPersonalSection)); } }
        public string? LastName { get => _lastName; private set { _lastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowPersonalSection)); } }
        public string? Address { get => _address; private set { _address = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowPersonalSection)); } }

        // Dane firmy (readonly w UI)
        public string? CompanyName { get => _companyName; private set { _companyName = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowCompanySection)); } }
        public string? CompanyNip { get => _companyNip; private set { _companyNip = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowCompanySection)); } }
        public string? CompanyAddress { get => _companyAddress; private set { _companyAddress = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowCompanySection)); } }

        private string? _firstName, _lastName, _address;
        private string? _companyName, _companyNip, _companyAddress;

        // Zmiana hasła
        private string? _pwMsg;
        private Brush? _pwBrush;
        public string? PasswordChangeMessage { get => _pwMsg; set { _pwMsg = value; OnPropertyChanged(); } }
        public Brush? PasswordChangeBrush { get => _pwBrush; set { _pwBrush = value; OnPropertyChanged(); } }

        public AccountViewModel(int userId)
        {
            _userId = userId;
            Username = UserService.GetUsername(userId);
            Email = UserService.GetEmail(userId);
            CreatedAt = UserService.GetCreatedAt(userId);

            // <- NOWE: typ konta z serwisu (jeśli nie masz takiej metody, zwróć Personal/Business jak już masz w UserService)
            AccountType = UserService.GetAccountType(userId);

            // Profil wprowadzony przy rejestracji/edycji
            var p = UserService.GetProfile(userId);
            FirstName = p.FirstName;
            LastName = p.LastName;
            Address = p.Address;

            CompanyName = p.CompanyName;
            CompanyNip = p.CompanyNip;
            CompanyAddress = p.CompanyAddress;

            ConnectBankCommand = new RelayCommand(_ => ConnectBank());
            DisconnectBankCommand = new RelayCommand(c =>
            {
                if (c is BankConnectionModel m)
                {
                    OpenBankingService.Disconnect(m.Id);
                    Load();
                }
            });
            SyncNowCommand = new RelayCommand(_ => { OpenBankingService.SyncNow(_userId); Load(); });

            Load();
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


