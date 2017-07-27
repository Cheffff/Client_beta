using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using PassportLogin.Models;
using PassportLogin.Utils;
using System.Diagnostics;
using PassportLogin.AuthService;
using Windows.Security.Credentials;
using PassportLogin.Pages;



// Pour plus d'informations sur le modèle d'élément Page vierge, voir la page http://go.microsoft.com/fwlink/?LinkId=234238

namespace PassportLogin.Views
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class Welcome : Page
    {
        private UserAccount _activeAccount;

        public Welcome()
        {
            this.InitializeComponent();
        }

        private void Button_Forget_User_Click(object sender, RoutedEventArgs e)
        {
            MicrosoftPassportHelper.RemovePassportAccountAsync(_activeAccount);

            Debug.WriteLine("User " + _activeAccount.Username + " deleted.");
            Frame.Navigate(typeof(UserSelection));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _activeAccount = (UserAccount)e.Parameter;
            if (_activeAccount != null)
            {
                UserAccount account = AuthService.AuthService.Instance.GetUserAccount(_activeAccount.UserId);
                if (account != null)
                {
                    UserListView.ItemsSource = account.PassportDevices;
                    UserNameText.Text = account.Username;
                }
            }
        }

        public static async void RemovePassportAccountAsync(UserAccount account)
        {
            //Open the account with Windows Hello
            KeyCredentialRetrievalResult keyOpenResult = await KeyCredentialManager.OpenAsync(account.Username);

            if (keyOpenResult.Status == KeyCredentialStatus.Success)
            {
                // In the real world you would send key information to server to unregister
                AuthService.AuthService.Instance.PassportRemoveUser(account.UserId);
            }

            //Then delete the account from the machines list of Passport Accounts
            await KeyCredentialManager.DeleteAsync(account.Username);
        }

        private void Button_Forget_Device_Click(object sender, RoutedEventArgs e)
        {
            PassportDevice selectedDevice = UserListView.SelectedItem as PassportDevice;
            if (selectedDevice != null)
            {
                //Remove it from Windows Hello
                MicrosoftPassportHelper.RemovePassportDevice(_activeAccount, selectedDevice.DeviceId);

                Debug.WriteLine("User " + _activeAccount.Username + " deleted.");

                if (!UserListView.Items.Any())
                {
                    //Navigate back to UserSelection page.
                    Frame.Navigate(typeof(UserSelection));
                }
            }
            else
            {
                ForgetDeviceErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Menu));
        }
    }
}
