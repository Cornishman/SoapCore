using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using SoapCore.MessageEncoder;
using SoapCore.Meta;
using SoapCore.ServiceModel;
using SoapCore.Tests.Serialization.Models.Xml;
using SoapCore.Tests.Wsdl.Services;
using SoapCore.Tests.Wsdl.Services.MessageContracts;

namespace SoapCore.Tests.Wsdl
{
	[TestClass]
	public class WsdlTests
	{
		private readonly XNamespace _xmlSchema = Namespaces.XMLNS_XSD;
		private readonly XNamespace _wsdlSchema = Namespaces.WSDL_NS;
		private readonly XNamespace _soapSchema = Namespaces.SOAP11_NS;

		private IWebHost _host;

		[TestMethod]
		public void CheckTaskReturnMethod()
		{
			StartService(typeof(TaskNoReturnService));
			var wsdl = GetWsdl();
			Trace.TraceInformation(wsdl);
			Assert.IsNotNull(wsdl);
			StopServer();
		}

		[TestMethod]
		public void CheckDataContractContainsItself()
		{
			StartService(typeof(DataContractContainsItselfService));
			var wsdl = GetWsdl();
			Trace.TraceInformation(wsdl);
			Assert.IsNotNull(wsdl);
			StopServer();
		}

		[TestMethod]
		public void CheckDataContractCircularReference()
		{
			StartService(typeof(DataContractCircularReferenceService));
			var wsdl = GetWsdl();
			Trace.TraceInformation(wsdl);
			Assert.IsNotNull(wsdl);
			StopServer();
		}

		[TestMethod]
		public void CheckNullableEnum()
		{
			StartService(typeof(NullableEnumService));
			var wsdl = GetWsdl();
			StopServer();

			// Parse wsdl content as XML
			var root = XElement.Parse(wsdl);

			// We should have in the wsdl the definition of a complex type representing the nullable enum
			var complexTypeElements = GetElements(root, _xmlSchema + "complexType").Where(a => a.Attribute("name")?.Value.Equals("NullableOfNulEnum") == true).ToList();
			complexTypeElements.ShouldNotBeEmpty();

			// We should have in the wsdl the definition of a simple type representing the enum
			var simpleTypeElements = GetElements(root, _xmlSchema + "simpleType").Where(a => a.Attribute("name")?.Value.Equals("NulEnum") == true).ToList();
			simpleTypeElements.ShouldNotBeEmpty();
		}

		[TestMethod]
		public void CheckNonNullableEnum()
		{
			StartService(typeof(NonNullableEnumService));
			var wsdl = GetWsdl();
			StopServer();

			// Parse wsdl content as XML
			var root = XElement.Parse(wsdl);

			// We should not have in the wsdl any definition of a complex type representing a nullable enum
			var complexTypeElements = GetElements(root, _xmlSchema + "complexType").Where(a => a.Attribute("name")?.Value.Equals("NullableOfNulEnum") == true).ToList();
			complexTypeElements.ShouldBeEmpty();

			// We should have in the wsdl the definition of a simple type representing the enum
			var simpleTypeElements = GetElements(root, _xmlSchema + "simpleType").Where(a => a.Attribute("name")?.Value.Equals("NulEnum") == true).ToList();
			simpleTypeElements.ShouldNotBeEmpty();
		}

		[TestMethod]
		public void CheckStructsInList()
		{
			StartService(typeof(StructService));
			var wsdl = GetWsdl();
			StopServer();
			var root = XElement.Parse(wsdl);
			var elementsWithEmptyName = GetElements(root, _xmlSchema + "element").Where(x => x.Attribute("name")?.Value == string.Empty);
			elementsWithEmptyName.ShouldBeEmpty();

			var elementsWithEmptyType = GetElements(root, _xmlSchema + "element").Where(x => x.Attribute("type")?.Value == "xs:");
			elementsWithEmptyType.ShouldBeEmpty();

			var structTypeElement = GetElements(root, _xmlSchema + "complexType").Single(x => x.Attribute("name")?.Value == "AnyStruct");
			var annotationNode = structTypeElement.Descendants(_xmlSchema + "annotation").SingleOrDefault();
			var isValueTypeElement = annotationNode.Descendants(_xmlSchema + "appinfo").Descendants(XNamespace.Get("http://schemas.microsoft.com/2003/10/Serialization/") + "IsValueType").SingleOrDefault();
			Assert.IsNotNull(isValueTypeElement);
			Assert.AreEqual("true", isValueTypeElement.Value);
			Assert.IsNotNull(annotationNode);
		}

		[TestMethod]
		public void CheckStreamDeclaration()
		{
			StartService(typeof(StreamService));
			var wsdl = GetWsdl();
			StopServer();
			var root = new XmlDocument();
			root.LoadXml(wsdl);

			var nsmgr = new XmlNamespaceManager(root.NameTable);
			nsmgr.AddNamespace("wsdl", "http://schemas.xmlsoap.org/wsdl/");
			nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");

			var element = root.SelectSingleNode("/wsdl:definitions/wsdl:types/xs:schema/xs:element[@name='GetStreamResponse']/xs:complexType/xs:sequence/xs:element", nsmgr);

			Assert.IsNotNull(element);
			Assert.AreEqual("StreamBody", element.Attributes["name"].Value);
			Assert.AreEqual("xs:base64Binary", element.Attributes["type"].Value);
		}

		[TestMethod]
		public void CheckDataContractName()
		{
			StartService(typeof(DataContractNameService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);
			var childRenamed = GetElements(root, _xmlSchema + "complexType").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("ChildRenamed") == true);
			Assert.IsNotNull(childRenamed);

			var extension = GetElements(childRenamed, _xmlSchema + "extension").SingleOrDefault(a => a.Attribute("base")?.Value.Equals("tns:BaseRenamed") == true);
			Assert.IsNotNull(extension);
		}

		[TestMethod]
		public void CheckEnumList()
		{
			StartService(typeof(EnumListService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);
			var listResponse = GetElements(root, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("ListResult") == true);
			Assert.IsNotNull(listResponse);
			Assert.AreEqual("q1:ArrayOfTestEnum", listResponse.Attribute("type").Value);

			var arrayOfTestEnum = GetElements(root, _xmlSchema + "complexType").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("ArrayOfTestEnum") == true);
			Assert.IsNotNull(arrayOfTestEnum);

			var element = GetElements(arrayOfTestEnum, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("TestEnum") == true);
			Assert.IsNotNull(element);
			Assert.AreEqual("0", element.Attribute("minOccurs").Value);
			Assert.AreEqual("unbounded", element.Attribute("maxOccurs").Value);
		}

		[TestMethod]
		public void CheckCollectionDataContract()
		{
			StartService(typeof(CollectionDataContractService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			var listStringsResult = GetElements(root, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("ListStringsResult") == true);
			Assert.IsNotNull(listStringsResult);
			Assert.AreEqual("http://testnamespace.org", listStringsResult.Attribute(XNamespace.Xmlns + "q1").Value);
			Assert.AreEqual("q1:MystringList", listStringsResult.Attribute("type").Value);

			var myStringList = GetElements(root, _xmlSchema + "complexType").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("MystringList") == true);
			Assert.IsNotNull(myStringList);

			var myStringElement = GetElements(myStringList, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("MyItem") == true);
			Assert.IsNotNull(myStringElement);

			var listMyTypesResult = GetElements(root, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("ListMyTypesResult") == true);
			Assert.IsNotNull(listMyTypesResult);
			Assert.AreEqual("http://testnamespace.org", listMyTypesResult.Attribute(XNamespace.Xmlns + "q2").Value);
			Assert.AreEqual("q2:MyMyTypeList", listMyTypesResult.Attribute("type").Value);

			var myMyTypeList = GetElements(root, _xmlSchema + "complexType").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("MyMyTypeList") == true);
			Assert.IsNotNull(myMyTypeList);

			var myMyTypeElement = GetElements(myMyTypeList, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("MyItem") == true);
			Assert.IsNotNull(myMyTypeElement);
		}

		[TestMethod]
		public void CheckMessageContractHeaderTypeGeneration()
		{
			StartService(typeof(MessageContractService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			var headerType = GetElements(root, _xmlSchema + "complexType").SingleOrDefault(a => a.Attribute("name")?.Value.Equals(nameof(MessageContractService.TestMessageContract)) == true);
			var headerTypeElements = GetElements(headerType, _xmlSchema + "element");

			// We should not have any header elements within the MessageContract ComplexType
			Assert.IsFalse(headerTypeElements.Any(e => e.Attribute("name").Value.Equals(nameof(MessageContractService.TestMessageContract.ComplexHeader))));
			Assert.IsFalse(headerTypeElements.Any(e => e.Attribute("name").Value.Equals(nameof(MessageContractService.TestMessageContract.HeaderField))));
			Assert.AreEqual(2, headerTypeElements.Count);
		}

		[TestMethod]
		public void CheckMessageContractHeaderMessageGeneration()
		{
			StartService(typeof(MessageContractService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			var headerMessage = GetElements(root, _wsdlSchema + "message").SingleOrDefault(a => a.Attribute("name")?.Value.Equals($"{nameof(IMessageContractService)}_{nameof(IMessageContractService.Test)}_Headers") == true);
			Assert.IsNotNull(headerMessage);
			var headerMessageParts = GetElements(headerMessage, _wsdlSchema + "part");

			// We should have header parts with names under the message node
			Assert.IsTrue(headerMessageParts.Any(e => e.Attribute("name").Value.Equals(nameof(MessageContractService.TestMessageContract.HeaderField))));
			Assert.IsTrue(headerMessageParts.Any(e => e.Attribute("name").Value.Equals(nameof(MessageContractService.TestMessageContract.ComplexHeader))));
			Assert.AreEqual(2, headerMessageParts.Count);
		}

		[TestMethod]
		public void CheckMessageContractBindingGeneration()
		{
			StartService(typeof(MessageContractService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			var serviceBinding = GetElements(root, _wsdlSchema + "binding").SingleOrDefault(a => a.Attribute("type")?.Value.Equals($"tns:{nameof(IMessageContractService)}") == true);
			Assert.IsNotNull(serviceBinding);
			var serviceBindingOperation = GetElements(serviceBinding, _wsdlSchema + "operation").SingleOrDefault(a => a.Attribute("name")?.Value.Equals(nameof(IMessageContractService.Test)) == true);
			Assert.IsNotNull(serviceBindingOperation);

			var inputElement = GetElements(serviceBindingOperation, _wsdlSchema + "input").SingleOrDefault();
			Assert.IsNotNull(inputElement);

			// We should have 2 header inputs for the required fields
			var inputHeaders = GetElements(inputElement, _soapSchema + "header");
			Assert.AreEqual(2, inputHeaders.Count);
			Assert.IsTrue(inputHeaders.Single(e => e.Attribute("part").Value.Equals(nameof(MessageContractService.TestMessageContract.HeaderField))) != null);
			Assert.IsTrue(inputHeaders.Single(e => e.Attribute("part").Value.Equals(nameof(MessageContractService.TestMessageContract.ComplexHeader))) != null);

			var outputElement = GetElements(serviceBindingOperation, _wsdlSchema + "output").SingleOrDefault();
			Assert.IsNotNull(outputElement);

			// We should have 2 header outputs for the required fields
			var outputHeaders = GetElements(outputElement, _soapSchema + "header");
			Assert.AreEqual(2, outputHeaders.Count);
			Assert.IsTrue(outputHeaders.Single(e => e.Attribute("part").Value.Equals(nameof(MessageContractService.TestMessageContract.HeaderField))) != null);
			Assert.IsTrue(outputHeaders.Single(e => e.Attribute("part").Value.Equals(nameof(MessageContractService.TestMessageContract.ComplexHeader))) != null);
		}

		[TestMethod]
		public void CheckMessageContractWrapperName()
		{
			StartService(typeof(MessageContractWrapperNameService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			// We should have an operation for test
			var contractOperationRenamed = GetElements(root, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals(nameof(IMessageContractWrapperNameService.Test)) == true);
			Assert.IsNotNull(contractOperationRenamed);

			// We should have an element with the name "WrapperRenamed"
			var operationElement = GetElements(contractOperationRenamed, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("WrapperRenamed") == true);
			Assert.IsNotNull(operationElement);

			// We should have a type NOT using the wrapper name
			Assert.IsFalse(operationElement.Attribute("type").Value.Equals("q1:WrapperRenamed"));
		}

		[TestMethod]
		public void CheckMessageContractWrapperNamespace()
		{
			StartService(typeof(MessageContractWrapperNamespaceService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			// We should have an operation for test
			var contractOperationRenamed = GetElements(root, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals(nameof(IMessageContractWrapperNamespaceService.Test)) == true);
			Assert.IsNotNull(contractOperationRenamed);

			// We should have an element called request for the test operation
			var operationElement = GetElements(contractOperationRenamed, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("request") == true);
			Assert.IsNotNull(operationElement);

			// The test element should have an xlmns attribute for the wrapper namespace
			Assert.IsTrue(operationElement.GetNamespaceOfPrefix("q1").NamespaceName.Equals(ServiceNamespace.Value));

			// There should be a schema defined for the wrapper namespace
			var wrapperSchema = GetElements(root, _xmlSchema + "schema").SingleOrDefault(a => a.Attribute("targetNamespace")?.Value.Equals(ServiceNamespace.Value) == true);
			Assert.IsNotNull(wrapperSchema);
		}

		[TestMethod]
		public void CheckMessageHeaderName()
		{
			StartService(typeof(MessageHeaderNameService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			var headerMessage = GetElements(root, _wsdlSchema + "message").SingleOrDefault(a => a.Attribute("name")?.Value.Equals($"{nameof(IMessageHeaderNameService)}_{nameof(IMessageHeaderNameService.Test)}_Headers") == true);
			Assert.IsNotNull(headerMessage);
			var headerMessageParts = GetElements(headerMessage, _wsdlSchema + "part");

			// We should have header part with renamed name under the message node
			Assert.IsNotNull(headerMessageParts.SingleOrDefault(e => e.Attribute("name").Value.Equals("HeaderRenamed")));

			// We should also have a binding input with the same header
			var testBinding = GetElements(root, _wsdlSchema + "binding").SingleOrDefault();
			Assert.IsNotNull(testBinding);
			var testBindingOperation = GetElements(testBinding, _wsdlSchema + "operation").SingleOrDefault(a => a.Attribute("name")?.Value.Equals(nameof(IMessageHeaderNameService.Test)) == true);
			Assert.IsNotNull(testBindingOperation);
			var testBindingInput = GetElements(testBindingOperation, _wsdlSchema + "input").Single();
			Assert.IsNotNull(testBindingInput);
			Assert.IsNotNull(GetElements(testBindingInput, _soapSchema + "header").SingleOrDefault(a => a.Attribute("part")?.Value.Equals("HeaderRenamed") == true));
		}

		[TestMethod]
		public void CheckMessageBodyMemberName()
		{
			StartService(typeof(MessageBodyMemberNameService));
			var wsdl = GetWsdl();
			StopServer();

			var root = XElement.Parse(wsdl);

			// We should have a complex type for the testContract
			var testContractComplexType = GetElements(root, _xmlSchema + "complexType").SingleOrDefault(a => a.Attribute("name")?.Value.Equals(nameof(MessageBodyMemberNameService.TestContract)) == true);
			Assert.IsNotNull(testContractComplexType);

			// We should have one element named using the member body member body name
			Assert.IsNotNull(GetElements(testContractComplexType, _xmlSchema + "element").SingleOrDefault(a => a.Attribute("name")?.Value.Equals("BodyMemberRenamed") == true));
		}

		[TestMethod]
		public async Task CheckDateTimeOffsetServiceWsdl()
		{
			var wsdl = await GetWsdlFromMetaBodyWriter<DateTimeOffsetService>();
			Trace.TraceInformation(wsdl);
			Assert.IsNotNull(wsdl);
		}

		[TestMethod]
		public async Task CheckXmlSchemaProviderTypeServiceWsdl()
		{
			var wsdl = await GetWsdlFromMetaBodyWriter<XmlSchemaProviderTypeService>();
			Trace.TraceInformation(wsdl);
			Assert.IsNotNull(wsdl);
		}

		[TestMethod]
		public async Task CheckTestMultipleTypesServiceWsdl()
		{
			var wsdl = await GetWsdlFromMetaBodyWriter<TestMultipleTypesService>();
			Trace.TraceInformation(wsdl);
			Assert.IsNotNull(wsdl);
		}

		[TestCleanup]
		public void StopServer()
		{
			_host?.StopAsync();
		}

		private string GetWsdl()
		{
			var serviceName = "Service.svc";

			var addresses = _host.ServerFeatures.Get<IServerAddressesFeature>();
			var address = addresses.Addresses.Single();

			using (var httpClient = new HttpClient())
			{
				return httpClient.GetStringAsync(string.Format("{0}/{1}?wsdl", address, serviceName)).Result;
			}
		}

		private async Task<string> GetWsdlFromMetaBodyWriter<T>()
		{
			var service = new ServiceDescription(typeof(T));
			var baseUrl = "http://tempuri.org/";
			var xmlNamespaceManager = Namespaces.CreateDefaultXmlNamespaceManager();
			var bodyWriter = new MetaBodyWriter(service, baseUrl, null, xmlNamespaceManager);
			var encoder = new SoapMessageEncoder(MessageVersion.Soap12WSAddressingAugust2004, System.Text.Encoding.UTF8, XmlDictionaryReaderQuotas.Max, false, true);
			var responseMessage = Message.CreateMessage(encoder.MessageVersion, null, bodyWriter);
			responseMessage = new MetaMessage(responseMessage, service, null, xmlNamespaceManager);

			var memoryStream = new MemoryStream();
			await encoder.WriteMessageAsync(responseMessage, memoryStream);
			memoryStream.Position = 0;

			var streamReader = new StreamReader(memoryStream);
			var result = streamReader.ReadToEnd();
			return result;
		}

		private void StartService(Type serviceType)
		{
			Task.Run(() =>
			{
				_host = new WebHostBuilder()
					.UseKestrel()
					.UseUrls("http://127.0.0.1:0")
					.ConfigureServices(services => services.AddSingleton<IStartupConfiguration>(new StartupConfiguration(serviceType)))
					.UseStartup<Startup>()
					.Build();

				_host.Run();
			});

			while (_host == null || _host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First().EndsWith(":0"))
			{
				Thread.Sleep(2000);
			}
		}

		private List<XElement> GetElements(XElement root, XName name)
		{
			var list = new List<XElement>();
			foreach (var xElement in root.Elements())
			{
				if (xElement.Name.Equals(name))
				{
					list.Add(xElement);
				}

				list.AddRange(GetElements(xElement, name));
			}

			return list;
		}
	}
}
