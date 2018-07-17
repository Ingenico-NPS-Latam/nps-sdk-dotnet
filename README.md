# .Net SDK

## Availability
Supports .Net 4.0 and above 


## How to install

```csharp
nuget install nps_sdk
```

## Configuration

It's a basic configuration of the SDK

```csharp
using NpsSdk;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.NpsEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger()));
```

Here is an simple example request:

```csharp
using NpsSdk;

try
    {
        RootElement data = new RootElement();

        data.Add("psp_Version", "2.2");
        data.Add("psp_MerchantId", "psp_test");
        data.Add("psp_TxSource", "WEB");
        data.Add("psp_MerchTxRef", "ORDER69461-3");
        data.Add("psp_MerchOrderId", "ORDER69461");
        data.Add("psp_Amount", "15050");
        data.Add("psp_NumPayments", "1");
        data.Add("psp_Currency", "032");
        data.Add("psp_Country", "ARG");
        data.Add("psp_Product", "14");
        data.Add("psp_CardNumber", "4507990000000010");
        data.Add("psp_CardExpDate", "1612");
        data.Add("psp_CardSecurityCode", "325");
        data.Add("psp_PosDateTime", "2016-12-01 12:00:00");
        RootElement response = npsSdk.PayOnLine_2p(data);
    }
catch (Exception ex)
    {
	//Code to handle error
    }
```

## Environments

```csharp
NpsSdk.NpsEnvironment.SandBox
NpsSdk.NpsEnvironment.Implementation
NpsSdk.NpsEnvironment.Production
```

## Error handling

Exceptions must be handled by user code, when LogLevel is set to Debug they will be logged

```csharp
//Code
try
{
	//code or sdk call
}
catch (Exception ex)
{
	//Code to handle error
}
```

## Advanced configurations

Nps SDK allows you to log what’s happening with you request inside of our SDK, it logs by default to System.Diagnostics.Debug

```csharp
using NpsSdk;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.NpsEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger()));
```


### Sanitize

Sanitize allows the SDK to truncate to a fixed size some fields that could make request fail, like extremely long name. (In this SDK it's done automatically)


If you prefer the sdk can write the output generated from the logger to the file you provided.

```csharp
using NpsSdk;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.NpsEnvironment.SandBox, "_YOUR_SECRET_KEY_", new FileLogger("path/to/your/file.log")));
```

### LogLevel

The LogLevel.Info level will write concise information of the request and will mask sensitive data of the request. 
The LogLevel.Debug level will write information about the request to let developers debug it in a more detailed way.

```csharp
using NpsSdk;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.NpsEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger()));
```

### Timeout
you can change the timeout of the request.

ExecutionTimeout(Default=60 seconds): you can change the execution timeout of the request.


```csharp
using NpsSdk;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.NpsEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger(), 60));
```

### Proxy

Proxy configuration, implementation of IWebProxy

```csharp
using NpsSdk;
using System.Net;

IWebProxy webProxy = new WebProxy();
var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.NpsEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger(), 60, webProxy));
```

### Tls Configuration (4.0)

Our servers uses TLS 1.2 Cryptographic Protocol

TLS Configuration .Net Framework 4.0: (it must be applied in the main application)


```csharp
using NpsSdk;
using System.Net;

System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
```

### Tls Configuration (4.5)

TLS Configuration .Net Framework 4.5+: (it must be applied in the main application only if the default value of the SecurityProtocol was previously changed)

```csharp
using NpsSdk;
using System.Net;

System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
```

