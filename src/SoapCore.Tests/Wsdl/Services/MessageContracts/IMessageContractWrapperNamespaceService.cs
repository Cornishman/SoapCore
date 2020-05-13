using System;
using System.ServiceModel;
using SoapCore.Tests.Serialization.Models.Xml;

namespace SoapCore.Tests.Wsdl.Services.MessageContracts
{
	[ServiceContract]
	public interface IMessageContractWrapperNamespaceService
	{
		[OperationContract]
		void Test(MessageContractWrapperNamespaceService.TestContract request);
	}

	public class MessageContractWrapperNamespaceService : IMessageContractWrapperNamespaceService
	{
		public void Test(TestContract request) => throw new NotImplementedException();

		[MessageContract(WrapperNamespace = ServiceNamespace.Value)]
		public class TestContract
		{
			[MessageBodyMember]
			public string StringField { get; set; }
		}
	}
}
