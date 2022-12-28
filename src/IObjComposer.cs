using System.Reflection;

namespace NP.IoC.CommonImplementations
{
    public interface IObjComposer
    {
        void ComposeObject(object obj);

        IEnumerable<object?> GetMethodParamValues(MethodBase methodInfo);
    }
}
