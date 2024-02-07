#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Web3;
using Nethereum.Web3.Accounts;

using Newtonsoft.Json;

public static class GsnClient
{
    public static async Task UpdateConfig(NetworkConfig config, GsnTransactionDetails transaction)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.RelayerApiKey ?? ""}");

        var response = await httpClient.GetAsync($"{config.Gsn.RelayUrl}/getaddr");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = await response.Content.ReadAsStringAsync();
        var serverConfigUpdate = GsnServerConfigPayload.FromJson(content);

        config.Gsn.RelayWorkerAddress = serverConfigUpdate.RelayWorkerAddress;
        SetGasFeesForTransaction(transaction, serverConfigUpdate);
    }

    public static async Task<RelayRequest> BuildRelayRequest(GsnTransactionDetails transaction, NetworkConfig config, Account wallet, Web3 web3Provider)
    {
        transaction.Gas = GnsTxHelper.EstimateGasWithoutCallData(transaction, config.Gsn.GtxDataNonZero, config.Gsn.GtxDataZero);

        var secondsNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validUntilTime = secondsNow + config.Gsn.RequestValidSeconds;

        var senderNonce = await GnsTxHelper.GetSenderNonce(wallet.Address, config.Gsn.ForwarderAddress, web3Provider);

        var forwardRequest = new ForwardRequest(transaction.From, transaction.To, transaction.Value ?? "0", transaction.Gas, senderNonce, transaction.Data, validUntilTime.ToString());
        var relayData = new RelayData(transaction.MaxFeePerGas, transaction.MaxPriorityFeePerGas, string.Empty, config.Gsn.RelayWorkerAddress, config.Gsn.PaymasterAddress, config.Gsn.ForwarderAddress, transaction.PaymasterData ?? "0x", "1");

        var relayRequest = new RelayRequest(forwardRequest, relayData);

        var transactionCalldataGasUsed = GnsTxHelper.EstimateCalldataCostForRequest(relayRequest, config.Gsn, web3Provider);
        relayRequest.RelayData.TransactionCalldataGasUsed = Convert.ToInt32(transactionCalldataGasUsed, 16).ToString();

        return relayRequest;
    }

    public static async Task<Dictionary<string, object>> BuildRelayHttpRequest(RelayRequest relayRequest, NetworkConfig config, Account account, Web3 web3Provider)
    {
        var signature = GnsTxHelper.SignRequest(relayRequest, config.Gsn.DomainSeparatorName, config.Gsn.ChainId, account, config);

        const string approvalData = "0x";

        var relayWorkerAddress = relayRequest.RelayData.RelayWorker;
        var relayLastKnownNonce = await web3Provider.Eth.Transactions.GetTransactionCount.SendRequestAsync(relayWorkerAddress);
        var relayMaxNonce = relayLastKnownNonce.Value + config.Gsn.MaxRelayNonceGap;

        var metadata = new Dictionary<string, object>
        {
            { "maxAcceptanceBudget", config.Gsn.MaxAcceptanceBudget },
            { "relayHubAddress", config.Gsn.RelayHubAddress },
            { "signature", signature },
            { "approvalData", approvalData },
            { "relayMaxNonce", relayMaxNonce },
            { "relayLastKnownNonce", relayLastKnownNonce.Value },
            { "domainSeparatorName", config.Gsn.DomainSeparatorName },
            { "relayRequestId", string.Empty }
        };

        var httpRequest = new Dictionary<string, object>
        {
            { "relayRequest", relayRequest.ToMap() },
            { "metadata", metadata }
        };

        return httpRequest;
    }

    public static async Task<string> RelayTransaction(Account account, NetworkConfig config, GsnTransactionDetails transaction)
    {
        var web3Provider = account.GetEthClient(config);
        await UpdateConfig(config, transaction);

        var relayRequest = await BuildRelayRequest(transaction, config, account, web3Provider);

        var httpRequest = await BuildRelayHttpRequest(relayRequest, config, account, web3Provider);

        var relayRequestId = GnsTxHelper.GetRelayRequestID((Dictionary<string, object>)httpRequest["relayRequest"], (string)((Dictionary<string, object>)httpRequest["metadata"])["signature"]);
        ((Dictionary<string, object>)httpRequest["metadata"])["relayRequestId"] = relayRequestId;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.RelayerApiKey ?? ""}");

        var jsonContent = JsonConvert.SerializeObject(httpRequest);
        var response = await httpClient.PostAsync($"{config.Gsn.RelayUrl}/relay", new StringContent(jsonContent, Encoding.UTF8, "application/json"));

        return await GnsTxHelper.HandleGsnResponse(response, web3Provider);
    }

    public static void SetGasFeesForTransaction(GsnTransactionDetails transaction, GsnServerConfigPayload serverConfigUpdate)
    {
        var serverSuggestedMinPriorityFeePerGas = BigInteger.Parse(serverConfigUpdate.MinMaxPriorityFeePerGas);
        var paddedMaxPriority = serverSuggestedMinPriorityFeePerGas * 140 / 100;
        transaction.MaxPriorityFeePerGas = paddedMaxPriority.ToString();

        if (serverConfigUpdate.ChainId == "80001")
        {
            transaction.MaxFeePerGas = paddedMaxPriority.ToString();
        }
        else
        {
            transaction.MaxFeePerGas = serverConfigUpdate.MaxMaxFeePerGas;
        }
    }

    public class GsnServerConfigPayload
    {
        public string RelayWorkerAddress { get; set; } = string.Empty;
        public string RelayManagerAddress { get; set; } = string.Empty;
        public string RelayHubAddress { get; set; } = string.Empty;
        public string OwnerAddress { get; set; } = string.Empty;
        public string MinMaxPriorityFeePerGas { get; set; } = string.Empty;
        public string MaxMaxFeePerGas { get; set; } = string.Empty;
        public string MinMaxFeePerGas { get; set; } = string.Empty;
        public string MaxAcceptanceBudget { get; set; } = string.Empty;
        public string ChainId { get; set; } = string.Empty;
        public string NetworkId { get; set; } = string.Empty;
        public bool Ready { get; set; }
        public string Version { get; set; } = string.Empty;

        public static GsnServerConfigPayload FromJson(string json)
        {
            return JsonConvert.DeserializeObject<GsnServerConfigPayload>(json) ?? new GsnServerConfigPayload();
        }
    }
}