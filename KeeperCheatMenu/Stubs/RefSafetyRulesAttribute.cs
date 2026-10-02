// Compiler-generated attribute stub.
// dnSpy exported this type because it lives in the assembly; the folder it used was
// derived from the namespace. Kept here so the netstandard2.1 build compiles without
// the compiler re-emitting the type. Not hand-written code.
using System;
using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class RefSafetyRulesAttribute : Attribute
	{
		public RefSafetyRulesAttribute(int A_1)
		{
			this.Version = A_1;
		}

		public readonly int Version;
	}
}
