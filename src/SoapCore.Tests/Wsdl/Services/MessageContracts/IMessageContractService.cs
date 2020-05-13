using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace SoapCore.Tests.Wsdl.Services.MessageContracts
{
	[ServiceContract]
	public interface IMessageContractService
	{
		[OperationContract]
		MessageContractService.TestMessageContract Test(MessageContractService.TestMessageContract request);
	}

	public class MessageContractService : IMessageContractService
	{
		public TestMessageContract Test(TestMessageContract request) => throw new NotImplementedException();

		[MessageContract]
		public class TestMessageContract
		{
			[MessageHeader]
			public string HeaderField { get; set; }

			[MessageHeader]
			public ComplexObject ComplexHeader { get; set; }

			[MessageBodyMember]
			public string BodyField { get; set; }

			[MessageBodyMember]
			public ComplexObject ComplexBody { get; set; }
		}

		[DataContract]
		public class ComplexObject
		{
			[DataMember]
			public string StringField1 { get; set; }

			[DataMember]
			public int IntField { get; set; }
		}
	}
}
