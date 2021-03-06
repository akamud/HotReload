﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Forms.HotReload.Extension.Helpers
{
    internal class HotReloadClientsHolder
    {
        private readonly HashSet<string> _addresses = new HashSet<string>();
        private readonly HttpClient _client;
        private readonly UdpReceiver _receiver;

        public event EventHandler<string> NewAddressAdded; 

        public HotReloadClientsHolder()
        {
            _client = new HttpClient();
            _receiver = new UdpReceiver();
            _receiver.Received += OnMessageReceived;
        }
        
        internal bool TryRun(out int port)
        {
            return _receiver.TryRun(out port);
        }

        internal void Stop()
        {
            _receiver.Stop();
        }

        private void OnMessageReceived(string addressMsg)
        {
            var address = addressMsg.Split(';').FirstOrDefault();

            if (address != null && !_addresses.Contains(address))
            {
                _addresses.Add(address);
                OnNewAddressAdded(address);
            }
        }

        internal async Task UpdateResourceAsync(string pathString, string contentString)
        {
            var escapedFilePath = Uri.EscapeDataString(pathString);
            var data = Encoding.UTF8.GetBytes(contentString);
            using (var content = new ByteArrayContent(data))
            {
                var sendTasks = _addresses
                    .Select(addr => _client.PostAsync($"{addr}/reload?path={escapedFilePath}", content)).ToArray();

                await Task.WhenAll(sendTasks).ConfigureAwait(false);
            }
        }

        protected virtual void OnNewAddressAdded(string e)
        {
            NewAddressAdded?.Invoke(this, e);
        }
    }
}