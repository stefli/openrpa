﻿using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenRPA.RDServicePlugin.Views
{
    /// <summary>
    /// Interaction logic for RunPluginView.xaml
    /// </summary>
    public partial class RunPluginView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        private Plugin plugin;
        public RunPluginView(Plugin plugin)
        {
            InitializeComponent();
            DataContext = this;
            this.plugin = plugin;
            lblWindowsusername.Text = NativeMethods.GetProcessUserName();
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Config.Save();
        }
        private void ReauthenticateButtonClick(object sender, RoutedEventArgs e)
        {
            if (Config.local.jwt != null && Config.local.jwt.Length > 0)
            {
                Log.Information("Saving temporart jwt token, from local settings.json");
                PluginConfig.tempjwt = new System.Net.NetworkCredential(string.Empty, Config.local.UnprotectString(Config.local.jwt)).Password;
                Config.Save();
            }
            else
            {
                Log.Error("Fail locating a JWT token to seed into service config!");
                MessageBox.Show("Fail locating a JWT token to seed into service config!");
            }
        }
        private async void StartServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableButtons();
                await Plugin.manager.StartService();
            }
            catch (Exception ex)
            {
                MessageBox.Show("StartServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private async void StopServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableButtons();
                await Plugin.manager.StopService();
                await Plugin.manager.StartService();
            }
            catch (Exception ex)
            {
                MessageBox.Show("StopServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private void InstallServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Plugin.manager.IsServiceInstalled)
                {
                    // if (string.IsNullOrEmpty(windowspassword.Password)) { MessageBox.Show("Password missing"); return; }
                    DisableButtons();
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    var filepath = asm.CodeBase.Replace("file:///", "");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    var filename = System.IO.Path.Combine(path, "OpenRPA.RDService.exe");
                    // Plugin.manager.InstallService(filename, new string[] { "username=" + NativeMethods.GetProcessUserName(), "password="+ windowspassword.Password });
                    Plugin.manager.InstallService(filename,null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("InstallServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private void UninstallServiceButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Plugin.manager.IsServiceInstalled)
                {
                    DisableButtons();
                    var asm = System.Reflection.Assembly.GetEntryAssembly();
                    var filepath = asm.CodeBase.Replace("file:///", "");
                    var path = System.IO.Path.GetDirectoryName(filepath);
                    var filename = System.IO.Path.Combine(path, "OpenRPA.RDService.exe");
                    Plugin.manager.UninstallService(filename);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UninstallServiceButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private unattendedclient client = null;
        public void DisableButtons()
        {
            AddcurrentuserButton.IsEnabled = false;
            RemovecurrentuserButton.IsEnabled = false;
            ReauthenticateButton.IsEnabled = false;
            StartServiceButton.IsEnabled = false;
            StopServiceButton.IsEnabled = false;
            InstallServiceButton.IsEnabled = false;
            UninstallServiceButton.IsEnabled = false;

        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string computername = NativeMethods.GetHostName().ToLower();
                string computerfqdn = NativeMethods.GetFQDN().ToLower();
                string windowsusername = NativeMethods.GetProcessUserName().ToLower();
                var clients = await global.webSocketClient.Query<unattendedclient>("openrpa", "{'_type':'unattendedclient', 'computername':'" + computername + "', 'computerfqdn':'" + computerfqdn + "', 'windowsusername':'" + windowsusername.Replace(@"\", @"\\") + "'}");
                AddcurrentuserButton.Content = "Add current user";
                if (clients.Length == 1)
                {
                    client = clients.First();
                    AddcurrentuserButton.Content = "Update current user";
                }
                chkUseFreeRDP.IsChecked = PluginConfig.usefreerdp;
                AddcurrentuserButton.IsEnabled = false;
                RemovecurrentuserButton.IsEnabled = false;
                ReauthenticateButton.IsEnabled = false;
                StartServiceButton.IsEnabled = false;
                StopServiceButton.IsEnabled = false;
                InstallServiceButton.IsEnabled = true;
                UninstallServiceButton.IsEnabled = false;
                if(client!=null)
                {
                    lblExecutable.Text = client.openrpapath;
                }
                if (Plugin.manager.IsServiceInstalled)
                {
                    AddcurrentuserButton.IsEnabled = true;
                    RemovecurrentuserButton.IsEnabled = (client!=null);
                    ReauthenticateButton.IsEnabled = true;
                    StartServiceButton.IsEnabled = (Plugin.manager.Status != System.ServiceProcess.ServiceControllerStatus.Running);
                    StopServiceButton.IsEnabled = (Plugin.manager.Status == System.ServiceProcess.ServiceControllerStatus.Running);

                    InstallServiceButton.IsEnabled = false;
                    UninstallServiceButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UserControl_Loaded: " + ex.Message);
            }

        }
        private async void AddcurrentuserButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableButtons();
                string computername = NativeMethods.GetHostName().ToLower();
                string computerfqdn = NativeMethods.GetFQDN().ToLower();
                string windowsusername = NativeMethods.GetProcessUserName().ToLower();
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                var path = asm.CodeBase.Replace("file:///", "");
                if (client == null)
                {
                    client = new unattendedclient() { computername = computername, computerfqdn = computerfqdn, windowsusername = windowsusername, name = computername + " " + windowsusername, openrpapath = path };
                    client = await global.webSocketClient.InsertOne("openrpa", 1, false, client);
                }
                lblExecutable.Text = client.openrpapath;
                if (!string.IsNullOrEmpty(windowspassword.Password)) client.windowspassword = windowspassword.Password;
                client.computername = computername;
                client.computerfqdn = computerfqdn;
                client.windowsusername = windowsusername;
                client.name = computername + " " + windowsusername;
                client.openrpapath = path;
                client = await global.webSocketClient.UpdateOne("openrpa", 1, false, client);
                windowspassword.Clear();
                plugin.reloadConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddcurrentuserButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }
        private async void RemovecurrentuserClick(object sender, RoutedEventArgs e)
        {
            if (client == null) return;
            try
            {
                DisableButtons();
                await global.webSocketClient.DeleteOne("openrpa", client._id);
                client = null;
                plugin.reloadConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddcurrentuserButtonClick: " + ex.Message);
            }
            finally
            {
                UserControl_Loaded(null, null);
            }
        }

        private void chkUseFreeRDP_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (chkUseFreeRDP.IsChecked == null) return;
            PluginConfig.usefreerdp = chkUseFreeRDP.IsChecked.Value;
            Config.Save();
        }

        private void chkUseFreeRDP_Click(object sender, RoutedEventArgs e)
        {
            if (chkUseFreeRDP.IsChecked == null) return;
            PluginConfig.usefreerdp = chkUseFreeRDP.IsChecked.Value;
            Config.Save();
        }
    }
}