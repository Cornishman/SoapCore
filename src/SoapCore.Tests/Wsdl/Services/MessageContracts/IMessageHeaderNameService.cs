using System;
using System.ServiceModel;

namespace SoapCore.Tests.Wsdl.Services.MessageContracts
{
	[ServiceContract]
	public interface IMessageHeaderNameService
	{
		[OperationContract]
		void Test(MessageHeaderNameService.TestContract request);
	}

	public class MessageHeaderNameService : IMessageHeaderNameService
	{
		public void Test(TestContract request) => throw new NotImplementedException();

		[MessageContract]
		public class TestContract
		{
			[MessageHeader(Name = "HeaderRenamed")]
			public string StringHeader { get; set; }
		}
	}
}
