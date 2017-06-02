using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Schema;

namespace NpsSDK
{
    public class NpsSdk
    {
        private const String SdkVersion = ".Net 1.0.0";

        #region Sanitize

        private static readonly Dictionary<String, Int32> FieldsMaxLength = new Dictionary<string, int> {
            {"psp_Person.FirstName",128},
            {"psp_Person.LastName",64},
            {"psp_Person.MiddleName",64},
            {"psp_Person.PhoneNumber1",32},
            {"psp_Person.PhoneNumber2",32},
            {"psp_Person.Gender",1},
            {"psp_Person.Nationality",3},
            {"psp_Person.IDNumber",40},
            {"psp_Person.IDType",5},
            {"psp_Address.Street",128},
            {"psp_Address.HouseNumber",32},
            {"psp_Address.AdditionalInfo",128},
            {"psp_Address.City",40},
            {"psp_Address.StateProvince",40},
            {"psp_Address.Country",3},
            {"psp_Address.ZipCode",10},
            {"psp_OrderItem.Description",127},
            {"psp_OrderItem.Type",20},
            {"psp_OrderItem.SkuCode",48},
            {"psp_OrderItem.ManufacturerPartNumber",30},
            {"psp_OrderItem.Risk",1},
            {"psp_Leg.DepartureAirport",3},
            {"psp_Leg.ArrivalAirport",3},
            {"psp_Leg.CarrierCode",2},
            {"psp_Leg.FlightNumber",5},
            {"psp_Leg.FareBasisCode",15},
            {"psp_Leg.FareClassCode",3},
            {"psp_Leg.BaseFareCurrency",3},
            {"psp_Passenger.FirstName",50},
            {"psp_Passenger.LastName",30},
            {"psp_Passenger.MiddleName",30},
            {"psp_Passenger.Type",1},
            {"psp_Passenger.Nationality",3},
            {"psp_Passenger.IDNumber",40},
            {"psp_Passenger.IDType",10},
            {"psp_Passenger.IDCountry",3},
            {"psp_Passenger.LoyaltyNumber",20},
            {"psp_SellerDetails.IDNumber",40},
            {"psp_SellerDetails.IDType",10},
            {"psp_SellerDetails.Name",128},
            {"psp_SellerDetails.Invoice",32},
            {"psp_SellerDetails.PurchaseDescription",32},
            {"psp_SellerDetails.MCC",5},
            {"psp_SellerDetails.ChannelCode",3},
            {"psp_SellerDetails.GeoCode",5},
            {"psp_TaxesRequest.TypeId",5},
            {"psp_MerchantAdditionalDetails.Type",1},
            {"psp_MerchantAdditionalDetails.SdkInfo",48},
            {"psp_MerchantAdditionalDetails.ShoppingCartInfo",48},
            {"psp_MerchantAdditionalDetails.ShoppingCartPluginInfo",48},
            {"psp_CustomerAdditionalDetails.IPAddress",45},
            {"psp_CustomerAdditionalDetails.AccountID",128},
            {"psp_CustomerAdditionalDetails.DeviceFingerPrint",4000},
            {"psp_CustomerAdditionalDetails.BrowserLanguage",2},
            {"psp_CustomerAdditionalDetails.HttpUserAgent",255},
            {"psp_BillingDetails.Invoice",32},
            {"psp_BillingDetails.InvoiceCurrency",3},
            {"psp_ShippingDetails.TrackingNumber",24},
            {"psp_ShippingDetails.Method",3},
            {"psp_ShippingDetails.Carrier",3},
            {"psp_ShippingDetails.GiftMessage",200},
            {"psp_AirlineDetails.TicketNumber",14},
            {"psp_AirlineDetails.PNR",10},
            {"psp_VaultReference.PaymentMethodToken",64},
            {"psp_VaultReference.PaymentMethodId",64},
            {"psp_VaultReference.CustomerId",64}
        };
        #endregion

        #region Attributes
        private readonly List<ServiceDefinition> _services;
        private readonly Dictionary<String, ComplexType> _types;
        private readonly WsdlHandlerConfiguration _wsdlHandlerConfiguration;

        public enum NpsEnvironment
        {
            SandBox,
            Staging,
            Production
        }

        #endregion

        #region Constructor

        public class WsdlHandlerConfiguration
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="logLevel"></param>
            /// <param name="npsEnvironment"></param>
            /// <param name="secretKey"></param>
            /// <param name="logger"></param>
            /// <param name="requestTimeout">The number of seconds to wait before the request times out. The default value is 100 seconds</param>
            /// <param name="proxy"></param>
            public WsdlHandlerConfiguration(LogLevel logLevel, NpsEnvironment npsEnvironment, String secretKey, ILogger logger = null, Int32 requestTimeout = 100, IWebProxy proxy = null)
            {
                _logLevel = logLevel;
                _npsEnvironment = npsEnvironment;
                _secretKey = secretKey;
                _requestTimeOut = requestTimeout;
                _proxy = proxy;
                _logger = new LogWrapper(logLevel, logger ?? new DebugLogger());
            }

            private NpsEnvironment _npsEnvironment;
            private LogLevel _logLevel;
            private String _secretKey;
            private LogWrapper _logger;
            private Int32 _requestTimeOut;
            private IWebProxy _proxy;

            internal NpsEnvironment NpsEnvironment
            {
                get { return _npsEnvironment; }
            }

            internal LogLevel LogLevel
            {
                get { return _logLevel; }
            }

            internal String SecretKey
            {
                get { return _secretKey; }
            }

            internal LogWrapper Logger
            {
                get { return _logger; }
            }

            internal Int32 RequestTimeout
            {
                get { return _requestTimeOut; }
            }

            internal IWebProxy Proxy
            {
                get { return _proxy; }
            }

            internal String LocalWsdlPath
            {
                get
                {
                    return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location) ?? String.Empty, String.Format("{0}.wsdl", NpsEnvironment.ToString().ToLower()));
                }
            }

            internal String ServiceUrl
            {
                get
                {
                    switch (NpsEnvironment)
                    {
                        case NpsEnvironment.SandBox:
                            return "https://sandbox.nps.com.ar/ws.php";
                        case NpsEnvironment.Staging:
                            return "https://implementacion.nps.com.ar/ws.php";
                        case NpsEnvironment.Production:
                            return "https://services2.nps.com.ar/ws.php";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public NpsSdk(WsdlHandlerConfiguration wsdlHandlerConfiguration)
        {
            try
            {
                if (wsdlHandlerConfiguration.LogLevel == LogLevel.Debug && wsdlHandlerConfiguration.NpsEnvironment == NpsEnvironment.Production)
                {
                    throw new ArgumentException("LogLevel can't be set to Debug on Production environment", "wsdlHandlerConfiguration");
                }

                _wsdlHandlerConfiguration = wsdlHandlerConfiguration;

                var pathWsdl = _wsdlHandlerConfiguration.LocalWsdlPath;
                if (!File.Exists(pathWsdl) && !DownloadWsdl(pathWsdl))
                {
                    throw new FileNotFoundException("Missing local WSDL");
                }

                var serviceDescription = ServiceDescription.Read(pathWsdl);

                _types = GetTypes(serviceDescription);

                _services = GetServices(serviceDescription, wsdlHandlerConfiguration);
            }
            catch (Exception ex)
            {
                _wsdlHandlerConfiguration.Logger.Log(LogLevel.Debug, ex.Message);
                throw;
            }
        }

        private List<ServiceDefinition> GetServices(ServiceDescription serviceDescription, WsdlHandlerConfiguration wsdlHandlerConfiguration)
        {
            var services = new List<ServiceDefinition>();
            foreach (PortType portType in serviceDescription.PortTypes)
            {
                foreach (Operation operation in portType.Operations)
                {
                    ServiceDefinition serviceDefinition = new ServiceDefinition(wsdlHandlerConfiguration)
                    {
                        ServiceName = operation.Name
                    };

                    foreach (OperationMessage message in operation.Messages)
                    {
                        var parameter = serviceDescription.Messages.Cast<Message>().FirstOrDefault(x => x.Name == message.Message.Name);

                        OperationInput operationInput = message as OperationInput;
                        if (operationInput != null)
                        {
                            serviceDefinition.InputType = parameter != null && parameter.Parts.Count > 0 ? parameter.Parts[0].Type.Name : String.Empty;
                            serviceDefinition.InputParameterName = parameter != null && parameter.Parts.Count > 0 ? parameter.Parts[0].Name : String.Empty;
                        }
                        OperationOutput operationOutput = message as OperationOutput;
                        if (operationOutput != null)
                        {
                            serviceDefinition.OutputType = parameter != null && parameter.Parts.Count > 0 ? parameter.Parts[0].Type.Name : String.Empty;
                            serviceDefinition.OutputParameterName = parameter != null && parameter.Parts.Count > 0 ? parameter.Parts[0].Name : String.Empty;
                        }
                    }
                    serviceDefinition.Input = GetTypeDefinition(serviceDefinition.InputType);
                    serviceDefinition.Output = GetTypeDefinition(serviceDefinition.OutputType);
                    services.Add(serviceDefinition);
                }
            }
            return services;
        }

        private static Dictionary<String, ComplexType> GetTypes(ServiceDescription serviceDescription)
        {
            var types = new Dictionary<String, ComplexType>();
            foreach (XmlSchemaObject item in serviceDescription.Types.Schemas[0].Items)
            {
                XmlSchemaComplexType xmlSchemaComplexType = item as XmlSchemaComplexType;
                if (xmlSchemaComplexType == null)
                {
                    continue;
                }

                var complexType = new ComplexType
                {
                    TypeName = xmlSchemaComplexType.Name,
                    IsArray = xmlSchemaComplexType.ContentModel != null && xmlSchemaComplexType.ContentModel.Content is XmlSchemaComplexContentRestriction && ((XmlSchemaComplexContentRestriction)xmlSchemaComplexType.ContentModel.Content).BaseTypeName.Name.ToLower() == "array",
                    IsMandatory = false,
                    Attributes = new List<Attribute>()
                };
                if (complexType.IsArray && xmlSchemaComplexType.ContentModel != null)
                {
                    complexType.TypeName = ((XmlSchemaAttribute)((XmlSchemaComplexContentRestriction)xmlSchemaComplexType.ContentModel.Content).Attributes[0]).UnhandledAttributes[0].OuterXml.Split(':').Last().Replace("[]\"", "");
                }

                OutputElements(complexType, xmlSchemaComplexType.Particle);

                types.Add(xmlSchemaComplexType.Name, complexType);
            }

            return types;
        }

        private class Node
        {
            public String NodeName { get; set; }
            public String NodeType { get; set; }
            public Node ArrayBaseType { get; set; }
            public Boolean IsArray { get; set; }
            public Boolean IsMandatory { get; set; }
            public List<Node> Children { get; set; }
            public Boolean IsSimpleType { get; set; }
        }

        private class ServiceDefinition
        {
            private readonly WsdlHandlerConfiguration _wsdlHandlerConfiguration;

            public ServiceDefinition(WsdlHandlerConfiguration wsdlHandlerConfiguration)
            {
                _wsdlHandlerConfiguration = wsdlHandlerConfiguration;
            }

            public String ServiceName { get; set; }
            internal String InputType { get; set; }
            internal String OutputType { get; set; }
            public String InputParameterName { get; set; }
            public String OutputParameterName { get; set; }

            public Node Input { get; set; }
            public Node Output { get; set; }

            private Boolean Validate(BaseElement data, out List<String> errorMessage)
            {
                return Validate(data, Input, String.Empty, out errorMessage);
            }

            private bool Validate(BaseElement data, Node parentNode, String path, out List<String> errors)
            {
                errors = new List<string>();

                List<BaseElement> dataChildren = data.Children.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
                List<Node> nodeChildren = parentNode.Children.OrderBy(x => x.NodeName, StringComparer.Ordinal).ToList();
                int dataCounter = 0, nodeCounter = 0;
                while (nodeCounter < nodeChildren.Count || dataCounter < dataChildren.Count)
                {
                    if (dataCounter > 0 && dataCounter < dataChildren.Count && dataChildren[dataCounter - 1].Name == dataChildren[dataCounter].Name)
                    {
                        errors.Add("Duplicate field: " + dataChildren[dataCounter].Name);
                        dataCounter++;
                        continue;
                    }

                    var compare = nodeCounter == nodeChildren.Count ? 1 : (dataCounter == dataChildren.Count ? -1 : String.Compare(nodeChildren[nodeCounter].NodeName, dataChildren[dataCounter].Name, StringComparison.Ordinal));

                    if (compare < 0)
                    {
                        ValidateMissingField(data, nodeChildren, path, nodeCounter, errors);
                        nodeCounter++;
                        continue;
                    }

                    if (compare > 0)
                    {
                        data.Children.Remove(dataChildren[dataCounter]);
                        //errors.Add("No matching field: " + dataChildren[dataCounter].Name);
                        dataCounter++;
                        continue;
                    }

                    ValidateMatchingField(dataChildren, nodeChildren, dataCounter, nodeCounter, parentNode, path, errors);

                    dataCounter++;
                    nodeCounter++;
                }
                return errors.Count == 0;
            }

            private void ValidateMatchingField(List<BaseElement> dataChildren, List<Node> nodeChildren, int dataCounter, int nodeCounter, Node parentNode, string path, List<string> errors)
            {
                if (!nodeChildren[nodeCounter].IsSimpleType != dataChildren[dataCounter] is ComplexElement)
                {
                    if (!nodeChildren[nodeCounter].IsArray || (dataChildren[dataCounter] is ComplexElementArray == false && dataChildren[dataCounter] is SimpleElementArray == false))
                    {
                        errors.Add("Wrong field type: " + dataChildren[dataCounter].Name);
                    }
                    else
                    {
                        if (nodeChildren[nodeCounter].IsArray)
                        {
                            if (dataChildren[dataCounter] is ComplexElementArray)
                            {
                                ((ComplexElementArray)dataChildren[dataCounter]).ChildType = nodeChildren[nodeCounter].ArrayBaseType.NodeType;
                            }
                            for (int i = 0; i < dataChildren[dataCounter].Children.Count; i++)
                            {
                                BaseElement arrayElement = dataChildren[dataCounter].Children[i];
                                if (arrayElement is ComplexElementArrayItem == nodeChildren[nodeCounter].ArrayBaseType.IsSimpleType)
                                {
                                    errors.Add("Wrong type in array: " + path + dataChildren[dataCounter].Name);
                                }
                                else
                                {
                                    List<string> innerErrors;
                                    if (!Validate(arrayElement, nodeChildren[nodeCounter].ArrayBaseType, String.Format("{0}{1}[{2}] --> ", path, nodeChildren[nodeCounter].NodeName, i), out innerErrors))
                                    {
                                        errors.AddRange(innerErrors);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (!nodeChildren[nodeCounter].IsSimpleType)
                {
                    List<String> innerErrors;
                    Validate(dataChildren[dataCounter], nodeChildren[nodeCounter], String.Format("{0}{1} --> ", path, nodeChildren[nodeCounter].NodeName), out innerErrors);
                    errors.AddRange(innerErrors);
                }
                else
                {
                    if (!String.IsNullOrWhiteSpace(parentNode.NodeName))
                    {
                        var simpleElement = dataChildren[dataCounter] as SimpleElement;
                        if (simpleElement != null)
                        {
                            String key = String.Format("{0}.{1}", parentNode.NodeName, simpleElement.Name);

                            if (FieldsMaxLength.ContainsKey(key))
                            {
                                simpleElement.Trim(FieldsMaxLength[key]);
                            }
                        }
                    }
                }
            }

            private void ValidateMissingField(BaseElement data, List<Node> nodeChildren, string path, int nodeCounter, List<string> errors)
            {
                if (nodeChildren[nodeCounter].IsMandatory)
                {
                    errors.Add("Missing field: " + path + nodeChildren[nodeCounter].NodeName);
                }
                if (nodeChildren[nodeCounter].NodeName == "psp_SecureHash" && data is RootElement &&
                    !data.Children.Exists(x => x.Name == "psp_ClientSession"))
                {
                    data.Children.Add(new SimpleElement("psp_SecureHash",
                        ((RootElement)data).SecureHash(_wsdlHandlerConfiguration.SecretKey)));
                }
                if (nodeChildren[nodeCounter].NodeName == "SdkInfo")
                {
                    data.Children.Add(new SimpleElement("SdkInfo", SdkVersion));
                }
                if (data is ComplexElement && nodeChildren[nodeCounter].NodeName == "psp_MerchantAdditionalDetails")
                {
                    ((ComplexElement)data).Add("psp_MerchantAdditionalDetails", new ComplexElement { { "SdkInfo", SdkVersion } });
                }
            }

            internal RootElement Call(BaseElement data)
            {
                List<String> errors;
                if (!Validate(data, out errors))
                {
                    throw new Exception(String.Join("\n", errors));
                }

                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(_wsdlHandlerConfiguration.ServiceUrl, ServiceName, InputParameterName, InputType, data);
                HttpWebRequest webRequest = CreateWebRequest();
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

                _wsdlHandlerConfiguration.Logger.LogRequest(LogLevel.Info, soapEnvelopeXml);

                RootElement rootElement = new RootElement();

                // get the response from the completed web request.
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    string soapResponse;
                    var responseStream = webResponse.GetResponseStream();
                    if (responseStream == null)
                    {
                        return null;
                    }

                    using (StreamReader rd = new StreamReader(responseStream))
                    {
                        soapResponse = rd.ReadToEnd();
                    }

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(soapResponse);

                    _wsdlHandlerConfiguration.Logger.LogResponse(LogLevel.Info, xmlDoc);

                    // Get elements
                    XmlNodeList outputElement = xmlDoc.GetElementsByTagName(OutputParameterName);
                    if (outputElement.Count != 1)
                    {
                        return null;
                    }

                    foreach (XmlNode childNode in outputElement[0].ChildNodes)
                    {
                        rootElement.Add(childNode.Name, Deserialize(childNode));
                    }
                }
                return rootElement;
            }

            private HttpWebRequest CreateWebRequest()
            {
                var action = _wsdlHandlerConfiguration.ServiceUrl + "/" + ServiceName;

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_wsdlHandlerConfiguration.ServiceUrl);

                if (_wsdlHandlerConfiguration.Proxy != null) { webRequest.Proxy = _wsdlHandlerConfiguration.Proxy; }
                webRequest.Timeout = _wsdlHandlerConfiguration.RequestTimeout * 1000;

                webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "POST";

                webRequest.Headers.Add("Pragma", "no-cache");
                webRequest.ProtocolVersion = HttpVersion.Version10;
                webRequest.KeepAlive = true;

                //webRequest.Headers.Add("Content-Length", webRequest.ContentLength.ToString());

                return webRequest;
            }
        }

        #endregion

        #region Private

        private class Attribute
        {
            public String AttributeName { get; set; }
            public String AttributeType { get; set; }
            public Boolean IsMandatory { get; set; }
        }

        private class ComplexType
        {
            public String TypeName { get; set; }
            public Boolean IsArray { get; set; }
            public Boolean IsMandatory { get; set; }
            public List<Attribute> Attributes { get; set; }
        }

        private static void OutputElements(ComplexType padre, XmlSchemaParticle particle)
        {
            XmlSchemaAll all = particle as XmlSchemaAll;
            if (all == null)
            {
                return;
            }

            foreach (XmlSchemaObject xmlSchemaObject in all.Items)
            {
                XmlSchemaElement childElement = xmlSchemaObject as XmlSchemaElement;

                if (childElement == null)
                {
                    continue;
                }

                padre.Attributes.Add(new Attribute
                {
                    AttributeName = childElement.Name,
                    IsMandatory = childElement.MinOccurs > 0,
                    AttributeType = childElement.SchemaTypeName.Name
                });
            }
        }

        private Node GetTypeDefinition(String typeName)
        {
            return GetTypeDefinition("", typeName);
        }

        private Node GetTypeDefinition(String nodeName, String nodeType)
        {
            var tipo = GetComplexType(nodeType);
            if (tipo == null)
            {
                return null;
            }

            var nodo = new Node
            {
                Children = new List<Node>(),
                IsArray = tipo.IsArray,
                IsMandatory = tipo.IsMandatory,
                NodeName = nodeName,
                NodeType = nodeType,
                ArrayBaseType = tipo.IsArray ? GetTypeDefinition(tipo.TypeName) : null,
                IsSimpleType = false
            };

            foreach (var attribute in tipo.Attributes)
            {
                var attributeDefinition = GetTypeDefinition(attribute.AttributeName, attribute.AttributeType);
                if (attributeDefinition != null)
                {
                    nodo.Children.Add(attributeDefinition);
                }
                else
                {
                    nodo.Children.Add(new Node
                    {
                        Children = null,
                        IsMandatory = attribute.IsMandatory,
                        NodeName = attribute.AttributeName,
                        IsArray = false,
                        NodeType = attribute.AttributeType,
                        ArrayBaseType = null,
                        IsSimpleType = true
                    });
                }
            }
            return nodo;
        }

        private ComplexType GetComplexType(String complexTypeName)
        {
            return _types.ContainsKey(complexTypeName) ? _types[complexTypeName] : null;
        }

        private RootElement Call(RootElement data, string memberName)
        {
            var serviceDefinition = _services.FirstOrDefault(x => x.ServiceName == memberName);
            if (serviceDefinition == null)
            {
                throw new Exception("Invalid service: " + memberName);
            }
            try
            {
                return serviceDefinition.Call(data);
            }
            catch (Exception ex)
            {
                _wsdlHandlerConfiguration.Logger.Log(LogLevel.Debug, ex.Message);
                throw;
            }
        }

        private Boolean DownloadWsdl(String outputPath)
        {
            //Build the URL request string
            UriBuilder uriBuilder = new UriBuilder(_wsdlHandlerConfiguration.ServiceUrl) { Query = "wsdl" };

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Method = "GET";
            webRequest.Accept = "text/xml";

            //Submit a web request to get the web service's WSDL
            using (WebResponse response = webRequest.GetResponse())
            {
                var stream = response.GetResponseStream();
                if (stream == null)
                {
                    return false;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    File.AppendAllText(outputPath, reader.ReadToEnd());
                }
            }
            return true;
        }

        #endregion

        #region SOAP Call

        private static BaseElement Deserialize(XmlNode xmlNode, Boolean arrayItem = false)
        {
            XmlElement xmlElement = xmlNode as XmlElement;

            if (xmlElement != null)
            {
                if (xmlElement.ChildNodes.Count == 1 && xmlElement.ChildNodes[0].NodeType == XmlNodeType.Text)
                {
                    return new SimpleElement(xmlNode.Name, xmlElement.ChildNodes[0].Value);
                }
                if (xmlElement.ChildNodes.Count > 0 && xmlElement.ChildNodes.Cast<XmlNode>().All(x => x.Name == "item"))
                {
                    Boolean isSimpleArray = true;
                    var items = new List<BaseElement>();
                    foreach (XmlNode childNode in xmlElement.ChildNodes)
                    {
                        var item = Deserialize(childNode, true);
                        isSimpleArray &= item is SimpleElement;
                        items.Add(item);
                    }
                    if (isSimpleArray)
                    {
                        var simpleElementArray = new SimpleElementArray { Name = xmlNode.Name };
                        foreach (var item in items) { simpleElementArray.Add(((SimpleElement)item).ConcatenatedValues); }
                        return simpleElementArray;
                    }
                    var complexElementArray = new ComplexElementArray { Name = xmlNode.Name };
                    foreach (var item in items) { complexElementArray.Add((ComplexElementArrayItem)item); }
                    return complexElementArray;
                }
                var complexElement = arrayItem ? new ComplexElementArrayItem() : new ComplexElement();
                complexElement.Name = xmlNode.Name;
                foreach (XmlNode childNode in xmlElement.ChildNodes)
                {
                    complexElement.Add(childNode.Name, Deserialize(childNode));
                }
                return complexElement;
            }
            throw new ArgumentException();
        }

        private static XmlDocument CreateSoapEnvelope(String url, String metodo, String nombreParametroInput, String tipoRequest, BaseElement data)
        {
            if (url.EndsWith(".php"))
            {
                url = url.Substring(0, url.Length - 4);
            }

            XmlDocument soapEnvelop = new XmlDocument();

            soapEnvelop.LoadXml(String.Format(@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:q2=""http://schemas.xmlsoap.org/soap/encoding/""><q1:{1} xmlns:q1=""{0}""><{2} href=""#id1""/></q1:{1}><q3:{3} id=""id1"" xsi:type=""q3:{3}"" xmlns:q3=""{0}"">{4}</q3:{3}></s:Body></s:Envelope>", url, metodo, nombreParametroInput, tipoRequest, data.Serialize()));

            return soapEnvelop;
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            try
            {
                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopeXml.Save(stream);
                }
            }
            catch
            {
                //WebExceptionStatus.ConnectFailure --> The remote service point could not be contacted at the transport level.
                //WebExceptionStatus.Timeout --> No response was received during the time-out period for a request
                throw;
                //throw new Exception("Communication exception, Status: " + ex.Status + ". See InnerException for further information.", ex);
            }
        }

        #endregion

        #region Services

        public RootElement Authorize_2p(RootElement data)
        {
            return Call(data, "Authorize_2p");
        }

        public RootElement Authorize_3p(RootElement data)
        {
            return Call(data, "Authorize_3p");
        }

        public RootElement BankPayment_2p(RootElement data)
        {
            return Call(data, "BankPayment_2p");
        }

        public RootElement BankPayment_3p(RootElement data)
        {
            return Call(data, "BankPayment_3p");
        }

        public RootElement Capture(RootElement data)
        {
            return Call(data, "Capture");
        }

        public RootElement CashPayment_3p(RootElement data)
        {
            return Call(data, "CashPayment_3p");
        }

        public RootElement ChangeSecretKey(RootElement data)
        {
            return Call(data, "ChangeSecretKey");
        }

        public RootElement CreateClientSession(RootElement data)
        {
            return Call(data, "CreateClientSession");
        }

        public RootElement CreatePaymentMethod(RootElement data)
        {
            return Call(data, "CreatePaymentMethod");
        }

        public RootElement CreatePaymentMethodFromPayment(RootElement data)
        {
            return Call(data, "CreatePaymentMethodFromPayment");
        }

        public RootElement CreatePaymentMethodToken(RootElement data)
        {
            return Call(data, "CreatePaymentMethodToken");
        }

        public RootElement DeletePaymentMethod(RootElement data)
        {
            return Call(data, "DeletePaymentMethod");
        }

        public RootElement FraudScreening(RootElement data)
        {
            return Call(data, "FraudScreening");
        }

        public RootElement GetIINDetails(RootElement data)
        {
            return Call(data, "GetIINDetails");
        }

        public RootElement GetInstallmentsOptions(RootElement data)
        {
            return Call(data, "GetInstallmentsOptions");
        }

        public RootElement NotifyFraudScreeningReview(RootElement data)
        {
            return Call(data, "NotifyFraudScreeningReview");
        }

        public RootElement PayOnLine_2p(RootElement data)
        {
            return Call(data, "PayOnLine_2p");
        }

        public RootElement PayOnLine_3p(RootElement data)
        {
            return Call(data, "PayOnLine_3p");
        }

        public RootElement QueryCardNumber(RootElement data)
        {
            return Call(data, "QueryCardNumber");
        }

        public RootElement QueryTxs(RootElement data)
        {
            return Call(data, "QueryTxs");
        }

        public RootElement Refund(RootElement data)
        {
            return Call(data, "Refund");
        }

        public RootElement RetrievePaymentMethod(RootElement data)
        {
            return Call(data, "RetrievePaymentMethod");
        }

        public RootElement RetrievePaymentMethodToken(RootElement data)
        {
            return Call(data, "RetrievePaymentMethodToken");
        }

        public RootElement SimpleQueryTx(RootElement data)
        {
            return Call(data, "SimpleQueryTx");
        }

        public RootElement SplitAuthorize_2p(RootElement data)
        {
            return Call(data, "SplitAuthorize_2p");
        }

        public RootElement SplitAuthorize_3p(RootElement data)
        {
            return Call(data, "SplitAuthorize_3p");
        }

        public RootElement SplitPayOnLine_2p(RootElement data)
        {
            return Call(data, "SplitPayOnLine_2p");
        }

        public RootElement SplitPayOnLine_3p(RootElement data)
        {
            return Call(data, "SplitPayOnLine_3p");
        }

        public RootElement RecachePaymentMethodToken(RootElement data)
        {
            return Call(data, "RecachePaymentMethodToken");
        }

        public RootElement UpdatePaymentMethod(RootElement data)
        {
            return Call(data, "UpdatePaymentMethod");
        }

        public RootElement CreateCustomer(RootElement data)
        {
            return Call(data, "CreateCustomer");
        }

        public RootElement UpdateCustomer(RootElement data)
        {
            return Call(data, "UpdateCustomer");
        }

        public RootElement DeleteCustomer(RootElement data)
        {
            return Call(data, "DeleteCustomer");
        }

        public RootElement RetrieveCustomer(RootElement data)
        {
            return Call(data, "RetrieveCustomer");
        }

        #endregion
    }
}
