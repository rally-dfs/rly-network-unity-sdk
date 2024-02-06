# Get Started - Rally Protocol

## Generate your API key
Navigate to [app.rallyprotocol.com](https://app.rallyprotocol.com/) to generate API keys for both Mumbai and production Polygon.

## Environments
### Unity
```c#
// Mumbai Network (Polygon Testnet Mumbai)
var mumbai = NetworkProvider.RlyMumbai;

// Polygon Network (Polygon Mainnet)
var mainnet = NetworkProvider.RlyPolygon;
```

## Initiating a Gasless Transaction
Once you have installed RallyMobile SDK, initialize it with your Mumbai API Key.

### Unity
```c#
// get Mumbai config for Rally Protocol SDK
var mumbai = NetworkProvider.RlyMumbai;

// add your API Key (can also be chained)
mumbai.WithApiKey(env.API_KEY);

// claim 10 test RLY tokens gaslessly for testing
await mumbai.ClaimRly();

// get balance of specified token
await mumbai.GetBalance(tokenAddress);

// transfer an ERC20 token
await mumbai.Transfer(
  transferAddress,
  new BigInteger(1),
  MetaTxMethod.ExecuteMetaTransaction
);
```

## Calling Contracts Not Natively Supported by the SDK
### Supported Contracts
The SDK allows sending transactions to the relayer API for contracts not supported by the SDK. The paymaster accepts two types of contracts:
1. ERC2771 compatible contracts
2. Non ERC2771 compatible contracts

### Using the Relay Function to Call Contracts
To gaslessly execute a transaction on a supported contract, create a GSN transaction object for your transaction and use the `relay()` method to send the transaction to our relayer.

### Unity
```c#
// Example Unity code for relaying a transaction
...
var gsnTx = new GsnTransactionDetails(
  from: accountAddress,
  data: tx.data,
  value: "0",
  to: contractAddress,
  gas: gas.ToString(),
  maxFeePerGas: maxFeePerGas.ToString(),
  maxPriorityFeePerGas: maxPriorityFeePerGas.ToString(),
);

await mumbai.Relay(gsnTx)
```

## References
View the [community libraries](https://docs.rallyprotocol.com/rallytransact/community-libraries) for more gasless transaction references.