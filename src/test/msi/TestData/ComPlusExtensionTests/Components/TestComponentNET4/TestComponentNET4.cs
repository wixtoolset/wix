using System;
using System.EnterpriseServices;
using System.Runtime.InteropServices;

[assembly: ApplicationActivation(ActivationOption.Library)]
namespace TestApplication
{
    /// <summary>
    /// TestComponent
    /// </summary>
    [ComVisible(true)]
    [Transaction(TransactionOption.Required)]
	[ObjectPooling(true, 5, 10)]
    [Guid("146AB3A2-4472-4DB9-94D5-311536E799BD")]
    public class TestComponent4 : ServicedComponent
	{
		public TestComponent4()
		{
			//
			// TODO: Add constructor logic here
			//
		}

        [AutoComplete(true)]
        public void TestMethod(string Name, string Address, int JobType, bool MakeFail)
		{

		}
	}
}
