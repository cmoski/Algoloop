﻿/*
 * Copyright 2018 Capnode AB
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Algoloop.Model;
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Algoloop.ViewModel
{
    public class MarketsViewModel : ViewModelBase
    {
        private readonly SettingService _settings;
        private ITreeViewModel _selectedItem;
        private bool _isBusy;

        public MarketsViewModel(MarketService markets, SettingService settings)
        {
            Model = markets;
            _settings = settings;

            AddCommand = new RelayCommand(() => DoAddMarket(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>((vm) => DoSelectedChanged(vm), (vm) => vm != null);

            DataFromModel();
        }

        public RelayCommand<ITreeViewModel> SelectedChangedCommand { get; }
        public RelayCommand AddCommand { get; }

        public MarketService Model { get; }
        public SyncObservableCollection<MarketViewModel> Markets { get; } = new SyncObservableCollection<MarketViewModel>();

        /// <summary>
        /// Mark ongoing operation
        /// </summary>
        public bool IsBusy
        {
            get =>_isBusy;
            set => Set(ref _isBusy, value);
        }

        public ITreeViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(ref _selectedItem, value);
            }
        }

        internal bool DoDeleteMarket(MarketViewModel market)
        {
            Debug.Assert(market != null);
            SelectedItem = null;
            return Markets.Remove(market);
        }

        public bool Read(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    using StreamReader r = new StreamReader(fileName);
                    string json = r.ReadToEnd();
                    Model.Copy(JsonConvert.DeserializeObject<MarketService>(json));

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().ToString());
                    return false;
                }
            }

            DataFromModel();
            return true;
        }

        internal bool Save(string fileName)
        {
            try
            {
                DataToModel();

                using StreamWriter file = File.CreateText(fileName);
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, Model);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        private void DoSelectedChanged(ITreeViewModel vm)
        {
            vm.Refresh();
            SelectedItem = vm;
        }

        private void DoAddMarket()
        {
            var loginViewModel = new MarketViewModel(this, new MarketModel(), _settings);
            Markets.Add(loginViewModel);
        }

        private void DataToModel()
        {
            Model.Markets.Clear();
            foreach (MarketViewModel market in Markets)
            {
                Model.Markets.Add(market.Model);
                market.DataToModel();
            }
        }

        private void DataFromModel()
        {
            Markets.Clear();
            foreach (MarketModel market in Model.Markets)
            {
                var viewModel = new MarketViewModel(this, market, _settings);
                Markets.Add(viewModel);
            }
        }
    }
}
