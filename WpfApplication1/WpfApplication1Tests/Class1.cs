using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mut = Microsoft.VisualStudio.TestTools.UnitTesting;
//using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
// some other usings

namespace WpfApplication1Tests
{

    public class ClassWithPrivateAsyncMethod
    {

        public bool Result = false;

        // Some stuff

        private async Task PrivateMethodAsync(string input)
        {
            // Call some async framework method
            await Task.Run(() =>
            {
                Thread.Sleep(1000);

                Result = true;
            });
            // do something with the result
        }

        // Some more stuff

    }

    [TestClass]
    public class ClassWithPrivateAsyncMethodTest
    {

        [TestMethod]
        public async Task TestPrivateMethodAsync()
        {
            // Create an instance of the class to be tested.
            ClassWithPrivateAsyncMethod instance = new ClassWithPrivateAsyncMethod();

            // Create a PrivateObject to access private methods of instance.
            PrivateObject privateObject = new PrivateObject(instance);

            // Create parameter values
            object[] args = new object[] { "SomeValue" };

            // Call the method to be tested via reflection, 
            // pass the parameter, and await the return
            await (Task)privateObject.Invoke("PrivateMethodAsync", args);

            Assert.IsTrue(instance.Result);
            // Do some validation stuff
        }

        // Some more stuff

    }

}