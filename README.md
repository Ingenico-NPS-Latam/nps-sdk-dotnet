### .Net SDK


####Availability
Supports .Net 4.5 and 4.6


####How to install

```
nuget install nps_sdk
```

####Configuration

It's a basic configuration of the SDK

```C#
using IngenicoSDK;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.IngenicoEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger()));
```



Here is an simple example request:

```C#
using IngenicoSDK;

try
{
	var response = npsSdk.PayOnLine_2p(
	    new RootElement
	     {
		{ "psp_Version", "2.2" },
		{ "psp_MerchantId", "psp_test" },
		{ "psp_TxSource", "WEB" },
		{ "psp_MerchTxRef", "ORDER69461-3" },
		{ "psp_MerchOrderId", "ORDER69461" },
		{ "psp_Amount", "15050" },
		{ "psp_NumPayments", "1" },
		{ "psp_Currency", "032" },
		{ "psp_Country", "ARG" },
		{ "psp_Product", "14" },
		{ "psp_CardNumber", "4507990000000010" },
		{ "psp_CardExpDate", "1612" },
		{ "psp_PosDateTime", "2016-12-01 12:00:00" },
		{ "psp_CardSecurityCode", "325" }
	     });
}
catch (Exception ex)
{
	#Code to handle error
}
```

####Environments

```C#
NpsSdk.IngenicoEnvironment.SandBox
NpsSdk.IngenicoEnvironment.Implementation
NpsSdk.IngenicoEnvironment.Production
```

####Error handling

Exceptions must be handled by user code, when LogLevel is set to Debug they will be logged

#Code
try
{
	#code or sdk call
}
catch (Exception ex)
{
	#Code to handle error
}
```

####Advanced configurations

Nps SDK allows you to log what’s happening with you request inside of our SDK, it logs by default to System.Diagnostics.Debug

```C#
using IngenicoSDK;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.IngenicoEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger()));
```


If you prefer the sdk can write the output generated from the logger to the file you provided.

```C#
using IngenicoSDK;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.IngenicoEnvironment.SandBox, "_YOUR_SECRET_KEY_", new FileLogger("path/to/your/file.log")));
```

The LogLevel.Info level will write concise information of the request and will mask sensitive data of the request. 
The LogLevel.Debug level will write information about the request to let developers debug it in a more detailed way.

```C#
using IngenicoSDK;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.IngenicoEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger()));
```

you can change the timeout of the request.

```C#
using IngenicoSDK;

var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.IngenicoEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger(), 60));
```

Proxy configuration, implementation of IWebProxy

```C#
using IngenicoSDK;
using System.Net;

IWebProxy webProxy = new WebProxy();
var npsSdk = new NpsSdk(new NpsSdk.WsdlHandlerConfiguration(LogLevel.Debug, NpsSdk.IngenicoEnvironment.SandBox, "_YOUR_SECRET_KEY_", new DebugLogger(), 60, webProxy));
```

