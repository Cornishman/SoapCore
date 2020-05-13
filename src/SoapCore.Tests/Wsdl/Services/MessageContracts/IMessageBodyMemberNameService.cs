using System;
using System.ServiceModel;

namespace SoapCore.Tests.Wsdl.Services.MessageContracts
{
	[ServiceContract]
	public interface IMessageBodyMemberNameService
	{
		[OperationContract]
		void Test(MessageBodyMemberNameService.TestContract request);
	}

	public class MessageBodyMemberNameService : IMessageBodyMemberNameService
	{
		public void Test(TestContract request) => throw new NotImplementedException();

		[MessageContract]
		public class TestContract
		{
			[MessageBodyMember(Name = "BodyMemberRenamed")]
			public string StringField { get; set; }
		}
	}
}
