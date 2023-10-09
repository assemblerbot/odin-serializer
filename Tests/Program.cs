// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Reflection.Emit;
using OdinSerializer;

[Serializable]
public class TestClassA
{
	public int    IntValue;
	public string StringValue = string.Empty;

	public virtual TestClassA Init()
	{
		IntValue    = 2;
		StringValue = "abc";
		return this;
	}
}

[Serializable]
public class TestClassB : TestClassA
{
	public float FloatValue;

	public override TestClassA Init()
	{
		FloatValue = 3;
		return base.Init();
	}
}

[Serializable]
public class TestClassC
{
	public List<TestClassA> TestList = new();
	public int              IntValue;

	public void Init()
	{
		TestList.Add(new TestClassA().Init());
		TestList.Add(new TestClassB().Init());
		TestList.Add(new TestClassA().Init());
		TestList.Add(new TestClassB().Init());
		IntValue = 5;
	}
}

[Serializable]
public class Test1
{
	public int IntValue;

	public void Init()
	{
		IntValue = 3;
	}
}

public class Program
{
	public static void Main(string[] args)
	{
		TestEmit3();
		Console.WriteLine("Serialize");
		
		TestClassC test1_in = new();
		test1_in.Init();
		byte[] json = SerializationUtility.SerializeValue(test1_in, DataFormat.JSON);
		Console.WriteLine(System.Text.Encoding.UTF8.GetString(json));
		
		Console.WriteLine("Deserialize");
		
		TestClassC test1_out = SerializationUtility.DeserializeValue<TestClassC>(json, DataFormat.JSON);
		
		Console.WriteLine("Done");
	}

	private static Type? CreateType1()
	{
		// Create an assembly.
		AssemblyName assemName = new AssemblyName();
		assemName.Name = "DynamicAssembly";
		AssemblyBuilder assemBuilder =
			AssemblyBuilder.DefineDynamicAssembly(assemName, AssemblyBuilderAccess.Run);
		// Create a dynamic module in Dynamic Assembly.
		ModuleBuilder modBuilder = assemBuilder.DefineDynamicModule("DynamicModule");
		// Define a public class named "DynamicClass" in the assembly.
		TypeBuilder typBuilder = modBuilder.DefineType("DynamicClass", TypeAttributes.Public);

		// Define a private String field named "DynamicField" in the type.
		FieldBuilder fldBuilder = typBuilder.DefineField("DynamicField",
			typeof(string), FieldAttributes.Private | FieldAttributes.Static);
		// Create the constructor.
		Type[]             constructorArgs = { typeof(String) };
		ConstructorBuilder constructor     = typBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorArgs);
		ILGenerator        constructorIL   = constructor.GetILGenerator();
		constructorIL.Emit(OpCodes.Ldarg_0);
		ConstructorInfo? superConstructor = typeof(Object).GetConstructor(new Type[0]);
		constructorIL.Emit(OpCodes.Call, superConstructor!);
		constructorIL.Emit(OpCodes.Ldarg_0);
		constructorIL.Emit(OpCodes.Ldarg_1);
		constructorIL.Emit(OpCodes.Stfld, fldBuilder);
		constructorIL.Emit(OpCodes.Ret);

		// Create the DynamicMethod method.
		MethodBuilder methBuilder = typBuilder.DefineMethod("DynamicMethod",
			MethodAttributes.Public, typeof(String), null);
		ILGenerator methodIL = methBuilder.GetILGenerator();
		methodIL.Emit(OpCodes.Ldarg_0);
		methodIL.Emit(OpCodes.Ldfld, fldBuilder);
		methodIL.Emit(OpCodes.Ret);

		Console.WriteLine($"Name               : {fldBuilder.Name}");
		Console.WriteLine($"DeclaringType      : {fldBuilder.DeclaringType}");
		Console.WriteLine($"Type               : {fldBuilder.FieldType}");
		return typBuilder.CreateType();
	}

	public static void TestEmit1()
	{
		Type? dynType = CreateType1();
		try
		{
			if (dynType is not null)
			{
				// Create an instance of the "HelloWorld" class.
				Object? helloWorld = Activator.CreateInstance(dynType, new Object[] { "HelloWorld" });
				// Invoke the "DynamicMethod" method of the "DynamicClass" class.
				Object? obj = dynType.InvokeMember("DynamicMethod",
					BindingFlags.InvokeMethod, null, helloWorld, null);
				Console.WriteLine($"DynamicClass.DynamicMethod returned: \"{obj}\"");
			}
		}
		catch (MethodAccessException e)
		{
			Console.WriteLine($"{e.GetType().Name}: {e.Message}");
		}
	}
    
	public static void TestEmit2()
	{
		// Create the "HelloWorld" class
		Type helloWorldType = CreateType2();
		Console.WriteLine("Full Name : " + helloWorldType.FullName);
		Console.WriteLine("Static constructors:");
		ConstructorInfo[] info =
			helloWorldType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic);
		for(int index=0; index < info.Length; index++)
			Console.WriteLine(info[index].ToString());
      
		// Print value stored in the static field
		Console.WriteLine(helloWorldType.GetField("Greeting").GetValue(null)); 
		Activator.CreateInstance(helloWorldType);
	}

	// Create the dynamic type.
	private static Type CreateType2()
	{
		AssemblyName myAssemblyName = new AssemblyName();
		myAssemblyName.Name = "EmittedAssembly";

		// Create the callee dynamic assembly.
		AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName,
			AssemblyBuilderAccess.Run);
		// Create a dynamic module named "CalleeModule" in the callee assembly.
		ModuleBuilder myModule = myAssembly.DefineDynamicModule("EmittedModule");

		// Define a public class named "HelloWorld" in the assembly.
		TypeBuilder helloWorldClass = myModule.DefineType("HelloWorld", TypeAttributes.Public);
		// Define a public static string field named "Greeting" in the type.
		FieldBuilder greetingField = helloWorldClass.DefineField("Greeting", typeof(String),
			FieldAttributes.Static | FieldAttributes.Public);

		// Create the static constructor.
		ConstructorBuilder constructor = helloWorldClass.DefineTypeInitializer();

		// Generate IL for the method. 
		// The constructor stores its "Hello emit!" in the public field.
		ILGenerator constructorIL = constructor.GetILGenerator();

		constructorIL.Emit(OpCodes.Ldstr,  "Hello emit!");
		constructorIL.Emit(OpCodes.Stsfld, greetingField);      
		constructorIL.Emit(OpCodes.Ret);

		return helloWorldClass.CreateType();
	}

	private          int     test;
	private delegate TReturn OneParameter<TReturn, TParameter0>(TParameter0 p0);
   
	public Program(int test) { this.test = test; }
   
	private static void TestEmit3()
	{
		// Example 2: A dynamic method bound to an instance.
		//
		// Create an array that specifies the parameter types for a
		// dynamic method. If the delegate representing the method
		// is to be bound to an object, the first parameter must
		// match the type the delegate is bound to. In the following
		// code the bound instance is of the Example class.
		//
		Type[] methodArgs2 = { typeof(Program), typeof(int) };

		// Create a DynamicMethod. In this example the method has no
		// name. The return type of the method is int. The method
		// has access to the protected and private data of the
		// Example class.
		//
		DynamicMethod multiplyHidden = new DynamicMethod(
			"",
			typeof(int),
			methodArgs2,
			typeof(Program));

		// Emit the method body. In this example ILGenerator is used
		// to emit the MSIL. DynamicMethod has an associated type
		// DynamicILInfo that can be used in conjunction with
		// unmanaged code generators.
		//
		// The MSIL loads the first argument, which is an instance of
		// the Example class, and uses it to load the value of a
		// private instance field of type int. The second argument is
		// loaded, and the two numbers are multiplied. If the result
		// is larger than int, the value is truncated and the most
		// significant bits are discarded. The method returns, with
		// the return value on the stack.
		//
		ILGenerator ilMH = multiplyHidden.GetILGenerator();
		ilMH.Emit(OpCodes.Ldarg_0);

		FieldInfo testInfo = typeof(Program).GetField("test", BindingFlags.NonPublic | BindingFlags.Instance);

		ilMH.Emit(OpCodes.Ldfld, testInfo);
		ilMH.Emit(OpCodes.Ldarg_1);
		ilMH.Emit(OpCodes.Mul);
		ilMH.Emit(OpCodes.Ret);

		// Create a delegate that represents the dynamic method.
		// Creating the delegate completes the method, and any further
		// attempts to change the method — for example, by adding more
		// MSIL — are ignored.
		//
		// The following code binds the method to a new instance
		// of the Example class whose private test field is set to 42.
		// That is, each time the delegate is invoked the instance of
		// Example is passed to the first parameter of the method.
		//
		// The delegate OneParameter is used, because the first
		// parameter of the method receives the instance of Example.
		// When the delegate is invoked, only the second parameter is
		// required.
		//
		OneParameter<int, int> invoke = (OneParameter<int, int>)
			multiplyHidden.CreateDelegate(
				typeof(OneParameter<int, int>),
				new Program(42)
			);

		Console.WriteLine("3 * test = {0}", invoke(3));
	}
}