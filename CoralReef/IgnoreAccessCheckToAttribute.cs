using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]

/*
 * DISCLAIMER : THIS CODE IS IN CASE YOU TRY TO BUILD THIS WITH A NON-PUBLICIZED(?) ASSEMBLY
 * OTHERWISE I THINK ITS IRRELEVANT
 * I *THINK* SO DON'T REMOVE IT JUST IN CASE
 * ELSE I'LL SLAP YOU
 */


namespace System.Runtime.CompilerServices {
    
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute {
        public IgnoresAccessChecksToAttribute(string assemblyName) {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}
