using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace SoapCore.Tests.Wsdl.Services.MessageContracts
{
	[ServiceContract]
	public interface IMessageContractWrapperNameService
	{
		[OperationContract]
		void Test(MessageContractWrapperNameService.TestContract request);
	}

	public class MessageContractWrapperNameService : IMessageContractWrapperNameService
	{
		public void Test(TestContract request) => throw new NotImplementedException();

		[MessageContract(WrapperName = "WrapperRenamed")]
		public class TestContract
		{
			[MessageBodyMember]
			public string StringField { get; set; }
		}
	}
}
