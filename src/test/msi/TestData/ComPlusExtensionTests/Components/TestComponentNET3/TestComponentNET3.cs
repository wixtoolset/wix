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
    [Guid("17F82C39-5433-493A-A396-36072C645B80")]
    public class TestComponent3 : ServicedComponent
	{
		public TestComponent3()
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
