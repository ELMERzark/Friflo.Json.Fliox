using System;
using System.Collections.Generic;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common
{

    public struct TestParserImpl
    {
	    public static void BasicJsonParser() {
		    JsonParser parser = new JsonParser();

		    using (var bytes = fromString("{}")) {
			    parser.InitParser(bytes);
			    Assert.AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
			    Assert.AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
			    AreEqual(0, parser.GetLevel());
			    AreEqual(JsonEvent.EOF, parser.NextEvent());
		    }

		    using (var bytes = fromString("{'test':'hello'}")) {
			    parser.InitParser(bytes);
			    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
			    AreEqual(JsonEvent.ValueString, parser.NextEvent());
			    AreEqual("test", parser.key.ToString());
			    AreEqual("hello", parser.value.ToString());
			    AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
			    AreEqual(0, parser.GetLevel());
			    AreEqual(JsonEvent.EOF, parser.NextEvent());
		    }

		    using (var bytes = fromString("{'a':'b','abc':123,'x':'ab\\r\\nc'}")) {
			    parser.InitParser(bytes);
			    AreEqual(JsonEvent.ObjectStart, parser.NextEvent());
			    AreEqual(JsonEvent.ValueString, parser.NextEvent());
			    AreEqual(JsonEvent.ValueNumber, parser.NextEvent());
			    AreEqual("abc", parser.key.ToString());
			    AreEqual("123", parser.value.ToString());
			    AreEqual(JsonEvent.ValueString, parser.NextEvent());
			    AreEqual("ab\r\nc", parser.value.ToString());
			    AreEqual(JsonEvent.ObjectEnd, parser.NextEvent());
			    AreEqual(0, parser.GetLevel());
			    AreEqual(JsonEvent.EOF, parser.NextEvent());
		    }

		    using (var bytes = fromString("[]")) {
			    parser.InitParser(bytes);
			    AreEqual(JsonEvent.ArrayStart, parser.NextEvent());
			    AreEqual(JsonEvent.ArrayEnd, parser.NextEvent());
			    AreEqual(0, parser.GetLevel());
			    AreEqual(JsonEvent.EOF, parser.NextEvent());
		    }
		    parser.Dispose();
	    }
	    
	    public static void TestParseFile(Bytes bytes)
		{
		//	ParseCx parseCx = new ParseCx();
			JsonParser parser = new JsonParser();
			parser.InitParser (bytes);									CheckPath(ref parser, "");
			AreEqual(JsonEvent.ObjectStart,	parser.NextEvent());
			AreEqual(JsonEvent.ValueString,	parser.NextEvent());		CheckPath(ref parser, "eur");
			AreEqual(">€<",					parser.value.ToString());
			AreEqual(JsonEvent.ValueString,	parser.NextEvent());		CheckPath(ref parser, "eur2");
			AreEqual("[€]",					parser.value.ToString());	
			
			AreEqual(JsonEvent.ValueNull,	parser.NextEvent());		CheckPath(ref parser, "null");
			AreEqual(JsonEvent.ValueBool,	parser.NextEvent());		CheckPath(ref parser, "true");
			AreEqual(true,					parser.boolValue);
			AreEqual(JsonEvent.ValueBool,	parser.NextEvent());		CheckPath(ref parser, "false");
			AreEqual(false,					parser.boolValue);
			
			AreEqual(JsonEvent.ObjectStart,	parser.NextEvent());		CheckPath(ref parser, "empty");
			AreEqual("empty",				parser.key.ToString());
			AreEqual(JsonEvent.ObjectEnd,	parser.NextEvent());		CheckPath(ref parser, "empty");
			
			AreEqual(JsonEvent.ObjectStart,	parser.NextEvent());		CheckPath(ref parser, "obj");
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "obj.val");
		//	AreEqual(11,					parser.number.ParseInt(parseCx));
			AreEqual(JsonEvent.ObjectEnd,	parser.NextEvent());		CheckPath(ref parser, "obj");
			
			AreEqual(JsonEvent.ArrayStart,	parser.NextEvent());		CheckPath(ref parser, "arr0[]");
			AreEqual("arr0",				parser.key.ToString());
			AreEqual(JsonEvent.ArrayEnd,	parser.NextEvent());		CheckPath(ref parser, "arr0");
			
			AreEqual(JsonEvent.ArrayStart,	parser.NextEvent());		CheckPath(ref parser, "arr1[]");
			AreEqual("arr1",				parser.key.ToString());
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "arr1[0]");
			AreEqual(JsonEvent.ArrayEnd,	parser.NextEvent());		CheckPath(ref parser, "arr1");
			
			AreEqual(JsonEvent.ArrayStart,	parser.NextEvent());		CheckPath(ref parser, "arr2[]");
			AreEqual("arr2",				parser.key.ToString());
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "arr2[0]");
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "arr2[1]");
			AreEqual(JsonEvent.ArrayEnd,	parser.NextEvent());		CheckPath(ref parser, "arr2");
			
			AreEqual(JsonEvent.ArrayStart,	parser.NextEvent());		CheckPath(ref parser, "arr3[]");
			AreEqual("arr3",				parser.key.ToString());
			AreEqual(JsonEvent.ObjectStart,	parser.NextEvent());		CheckPath(ref parser, "arr3[0]");
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "arr3[0].val");
			AreEqual(JsonEvent.ObjectEnd,	parser.NextEvent());		CheckPath(ref parser, "arr3[0]");		
			AreEqual(JsonEvent.ObjectStart,	parser.NextEvent());		CheckPath(ref parser, "arr3[1]");
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "arr3[1].val");
			AreEqual(JsonEvent.ObjectEnd,	parser.NextEvent());		CheckPath(ref parser, "arr3[1]");
			AreEqual(JsonEvent.ArrayEnd,	parser.NextEvent());		CheckPath(ref parser, "arr3");
			
			AreEqual(JsonEvent.ValueString,	parser.NextEvent());		CheckPath(ref parser, "str");
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "int32");
			AreEqual(JsonEvent.ValueNumber,	parser.NextEvent());		CheckPath(ref parser, "dbl");
			
			AreEqual(JsonEvent.ObjectEnd,	parser.NextEvent());		CheckPath(ref parser, "");
			AreEqual(JsonEvent.EOF,			parser.NextEvent());		CheckPath(ref parser, "");
		
			for (int n = 0; n < 1; n++)
			{
				int count = 0;
				parser.InitParser(bytes);
				while (parser.NextEvent() != JsonEvent.EOF)
					count++;
				IsTrue(count == 32);
			}
			parser.Dispose();
		}
	    
	    public static void CheckPath (ref JsonParser parser, String path)
	    {
		    AreEqual(path,		parser.GetPath());
	    }

	    
	    public static  Bytes fromString (String str) {

		    Bytes buffer = new Bytes(256);
		    str = str. Replace ('\'', '\"');
		    buffer.AppendString(str);
		    Bytes ret = buffer.SwapWithDefault();
		    buffer.Dispose();
		    return ret;
	    }
    }

    public class TestJsonParser : ECSLeakTestsFixture
    {
	    private PropType.Store createStore()
	    {
		    PropType.Store		store = new PropType.Store();
		    store.RegisterType("Sub", typeof( Sub ));
		    return store;
	    }
	    
        [Test]
        public void TestParser() {
	        TestParserImpl.BasicJsonParser();
        }

        [Test]
        public void TestParseFile() {
	        using (Bytes bytes = TestJsonParser.fromFile("assets/codec/parse.json")) {
		        TestParserImpl.TestParseFile(bytes);
	        }
        }
        
        String JsonSimpleObj = "{'val':5}";
        
        [Test]
        public void EncodeJsonSimple()	{
	        using (PropType.Store store = createStore())
	        using (Bytes bytes = TestParserImpl.fromString(JsonSimpleObj))
	        {
		        JsonSimple obj = (JsonSimple) EncodeJson(bytes, typeof(JsonSimple), store);
		        AreEqual(5L, obj.val);
	        }
        }

        [Test]
        public void ParseJsonComplex()	{
	        using (Bytes bytes = fromFile("assets/codec/complex.json")) {
		        ParseJson(bytes);
	        }
        }

        public static Bytes fromFile (String path)
        {
#if UNITY_5_3_OR_NEWER
	        string baseDir = UnityUtils.GetProjectFolder();
#else
	        string baseDir = Directory.GetCurrentDirectory() + "/../../../";	        
#endif
	        byte[] data = File.ReadAllBytes(baseDir + path);
		    ByteArray bytes = Arrays.CopyFrom(data);
	        return new Bytes(bytes);
        }
        
        int					num2 =				2;
        
        private void ParseJson(Bytes json)
        {
	        // 170 ms - 20000, Release | Any CPU, target framework: net5.0, complex.json: 1134 bytes => 133 MB/sec
	        using (JsonParser parser = new JsonParser()) {
		        // StopWatch stopwatch = new StopWatch();
		        for (int n = 0; n < num2; n++) {
			        parser.InitParser(json);
			        while (parser.NextEvent() != JsonEvent.EOF) {
			        }
		        }
	        }
	        // FFLog.log("ParseJson: " + json + " : " + stopwatch.Time());
        }
        
        private Object EncodeJson(Bytes json, Type type, PropType.Store store)
        {
	        Object ret = null;
	        using (var enc = new JsonReader(store)) {
		        // StopWatch stopwatch = new StopWatch();
		        for (int n = 0; n < num2; n++) {
			        ret = enc.Read(json, type);
			        if (ret == null)
				        throw new FrifloException(enc.Error.Msg.ToString());
		        }
		        // FFLog.log("EncodeJson: " + json + " : " + stopwatch.Time());
	        }
	        return ret;
        }
		
        private Object EncodeJsonTo(Bytes json, Object obj, PropType.Store store)
        {
	        Object ret = null;
	        using (JsonReader enc = new JsonReader(store)) {
		        // StopWatch stopwatch = new StopWatch();
		        for (int n = 0; n < num2; n++) {
			        ret = enc.ReadTo(json, obj);
			        if (ret == null)
				        throw new FrifloException(enc.Error.Msg.ToString());
		        }
		        // FFLog.log("EncodeJsonTo: " + json + " : " + stopwatch.Time());
		        return ret;
	        }
        }
        
        private static void checkMap (IDictionary <String, JsonSimple> map)
		{
			AreEqual (2, map. Count);
			JsonSimple key1 = map [ "key1" ];
			AreEqual (		   1L, key1.val);
			JsonSimple key2 = map [ "key2" ];
			AreEqual (		 null, key2);
		}
		
		private static void checkList (IList <Sub> list)
		{
			AreEqual (		     4, list. Count);
			AreEqual (		   11L, list [0] .i64);
			AreEqual (		  null, list [1] );
			AreEqual (		   13L, list [2] .i64);
			AreEqual (		   14L, list [3] .i64);
		}

		private static void checkJsonComplex (JsonComplex obj)
		{
			AreEqual (		   64L, obj.i64);
			AreEqual (			32, obj.i32);
			AreEqual ((short)	16, obj.i16);
			AreEqual ((byte)	 8, obj.i8);
			AreEqual (		   22d, obj.dbl);
			AreEqual (		 11.5f, obj.flt);
			AreEqual (  "string-ý", obj.str);
			AreEqual (        null, obj.strNull);
			AreEqual ("_\"_\\_\b_\f_\r_\n_\t_", obj.escChars);
			AreEqual (		  null, obj.n);
			AreEqual (		   99L, (( Sub)obj.subType).i64);
			AreEqual (		  true, obj.t);
			AreEqual (		 false, obj.f);
			AreEqual (		 	1L, obj.sub.i64);
			AreEqual (		   21L, obj.arr[0].i64);
			AreEqual (		  null, obj.arr[1]    );
			AreEqual (		   23L, obj.arr[2].i64);
			AreEqual (		   24L, obj.arr[3].i64);
			AreEqual (		     4, obj.arr. Length);
			checkList (obj.list);
			checkList (obj.list2);
			checkList (obj.list3);
			checkList (obj.list4);
			checkList (obj.listDerived);
			checkList (obj.listDerivedNull);
			AreEqual (		"str0",	obj.listStr [0] );
			AreEqual (		  101L,	((Sub)obj.listObj [0]) .i64 );
			checkMap (obj.map);
			checkMap (obj.map2);
			checkMap (obj.map3);
			checkMap (obj.map4);
			checkMap (obj.mapDerived);
			checkMap (obj.mapDerivedNull);
			AreEqual (		"str1", obj.map5 [ "key1" ]);
			AreEqual (		  null, obj.map5 [ "key2" ]);
		}
		
		private static void setMap (IDictionary <String, JsonSimple> map)
		{
			// order not defined for HashMap
			map [ "key1" ]= new  JsonSimple(1L) ;
			map [ "key2" ]= null ;
		}
		
		private static void setList (IList <Sub> list)
		{
			list. Add (new Sub(11L));
			list. Add (null);
			list. Add (new Sub(13L));
			list. Add (new Sub(14L));
		}
		
		private static void setComplex (JsonComplex obj)
		{
			obj.i64 = 64;
			obj.i32 = 32;
			obj.i16 = 16;
			obj.i8	= 8;
			obj.dbl = 22d;
			obj.flt = 11.5f;
			obj.str = "string-ý";
			obj.strNull = null;
			obj.escChars =  "_\"_\\_\b_\f_\r_\n_\t_";
			obj.n = null; 
			obj.subType = new Sub(99);
			obj.t = true;
			obj.f = false;
			obj.sub = new Sub();
			obj.sub.i64 = 1L;
			obj.arr = new Sub[4];
			obj.arr[0] = new Sub(21L);
			obj.arr[1]    = null;
			obj.arr[2] = new Sub(23L);
			obj.arr[3] = new Sub(24L);
			obj.list =  new List <Sub>();
			setList (obj.list);
			setList (obj.list2);
			obj.list3 =  new List <Sub>();
			setList (obj.list3);
			setList (obj.list4);
			setList (obj.listDerived);
			obj.listDerivedNull = new DerivedList();
			setList (obj.listDerivedNull);
			obj.listStr. Add ("str0");
			obj.listObj. Add (new Sub(101));
			obj.map = new Dictionary <String, JsonSimple>();
			setMap (obj.map);
			setMap (obj.map2);
			obj.map3 = new Dictionary <String, JsonSimple>();
			setMap (obj.map3);
			setMap (obj.map4);
			setMap (obj.mapDerived);
			obj.mapDerivedNull = new DerivedMap();
			setMap (obj.mapDerivedNull);
			obj.map5 = new Dictionary <String, String>();
			obj.map5 [ "key1" ] = "str1" ;
			obj.map5 [ "key2" ] = null ;
			obj.i64arr = new int[] {1, 2, 3};
		}
	
		
		[Test]
		public void EncodeJsonComplex() {
			using (PropType.Store store = createStore())
			using (Bytes bytes = fromFile("assets/codec/complex.json")) {
				JsonComplex obj = (JsonComplex) EncodeJson(bytes, typeof(JsonComplex), store);
				checkJsonComplex(obj);
			}
		}
		
		[Test]
		public void EncodeJsonToComplex()	{
			using (PropType.Store store = createStore())
			using (Bytes bytes = fromFile("assets/codec/complex.json")) {
				JsonComplex obj = new JsonComplex();
				obj = (JsonComplex) EncodeJsonTo(bytes, obj, store);
				checkJsonComplex(obj);
			}
		}
		
		[Test]
		public void WriteJsonComplex()
		{
			using (PropType.Store store = createStore()) {
				JsonComplex obj = new JsonComplex();
				setComplex(obj);
				using (JsonWriter writer = new JsonWriter(store)) {
					writer.Write(obj);

					using (JsonReader enc = new JsonReader(store)) {
						JsonComplex res = (JsonComplex) enc.Read(writer.Output, typeof(JsonComplex));
						if (res == null)
							Fail(enc.Error.Msg.ToString());
						checkJsonComplex(res);
					}
				}
			}
		}

		[Test]
		public void testUtf8() {
			Bytes src = fromFile ("assets/EuroSign.txt");
			String str = src.ToString();
			AreEqual("€", str);

			Bytes dst = new Bytes();
			dst.FromString("€");
			IsTrue(src.IsEqualBytes(dst));
			dst.Dispose();
			src.Dispose();
		}
		
		[Test]
		public void testStringIsEqual() {
			Bytes bytes = new Bytes("€");
			AreEqual(3, bytes.Len); // UTF-8 length of € is 3
			String eur = "€";
			AreEqual(eur, bytes.ToString());
			IsTrue(bytes.IsEqualString(eur));
			bytes.Dispose();
			//
			Bytes xyz = new Bytes("xyz");
			String abc = "abc";
			AreEqual(abc.Length, xyz.Len); // Ensure both have same UTF-8 length (both ASCII)
			AreNotEqual(abc, xyz.ToString());
			IsFalse(xyz.IsEqualString(abc));
			xyz.Dispose();			
		}
		
		[Test]
		public void testUtf8Compare() {
			using (var empty = new Bytes(""))
			using (var a = new Bytes("a"))
			using (var ab = new Bytes("ab"))
			using (var copyright = new Bytes("©"))		//  © U+00A9  
			using (var euro = new Bytes("€"))			//  € U+20AC
			using (var smiley = new Bytes("😎"))		//  😎 U+1F60E
			{
				IsTrue (Utf8Utils.IsStringEqualUtf8("", empty));
				IsTrue (Utf8Utils.IsStringEqualUtf8("a", a));

				IsFalse(Utf8Utils.IsStringEqualUtf8("a",  ab));
				IsFalse(Utf8Utils.IsStringEqualUtf8("ab", a));

				IsTrue (Utf8Utils.IsStringEqualUtf8("©", copyright));
				IsTrue (Utf8Utils.IsStringEqualUtf8("€", euro));
				IsTrue (Utf8Utils.IsStringEqualUtf8("😎", smiley));
			}
		}

		[Test]
		public void testBurstStringInterpolation() {
			using (Bytes bytes = new Bytes(128)) {
				int val = 42;
				int val2 = 43;
				char a = 'a';
				bytes.AppendFixed32($"With Bytes {val} {val2} {a}");
				AreEqual("With Bytes 42 43 a", $"{bytes.ToFixed32()}");

				var withValues = bytes.ToFixed32();
				String32 str32 = new String32("World");
				String128 err = new String128($"Hello {str32.value} {withValues}");
				AreEqual("Hello World With Bytes 42 43 a", err.value);
			}
		}
    }


    struct Struct
    {
	    public int val;
    }
    
    public class TestStructBehavior
    {
	    [Test]
	    public void testStructBehavior() {
		    Struct var1 = new Struct();
		    Struct var2 = var1; // copy as value;
		    ref Struct ref1 = ref var1; 
		    var1.val = 11;
		    AreEqual(0, var2.val); // copy still unchanged
		    AreEqual(11, ref1.val); // reference reflect changes
		    
		    modifyParam(var1);  // method parameter is copied as value, original value stay unchanged
		    AreEqual(11, ref1.val);
		    
		    modifyRefParam(ref var1);
		    AreEqual(12, ref1.val); // method parameter is given as reference value, original value is changed
	    }

	    private void modifyParam(Struct param) {
		    param.val = 12;
	    }
	    
	    private void modifyRefParam(ref Struct param) {
		    param.val = 12;
	    }
	    
	    // in parameter is passed as reference (ref readonly) - it must not be changed
	    // using in parameter degrade performance:
	    // [c# 7.2 - Why would one ever use the "in" parameter modifier in C#? - Stack Overflow] https://stackoverflow.com/questions/52820372/why-would-one-ever-use-the-in-parameter-modifier-in-c
	    private void passByReadOnlyRef(in Struct param) {
		    // param.val = 12;  // error CS8332: Cannot assign to a member of variable 'in Struct' because it is a readonly variable
	    }
    }


}