﻿using System;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#if NET6_0_OR_GREATER

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public class TestIL
    {
        [Test]
        public void DoubleToLong() {
            double dbl64 = 123.456;
            long   lng          = BitConverter.DoubleToInt64Bits(dbl64);
            double dblResult    = BitConverter.Int64BitsToDouble(lng);
            AreEqual(dbl64, dblResult);
            

            float  flt32 = 123.456f;
            int integer        = BitConverter.SingleToInt32Bits(flt32);
            float result32     = BitConverter.Int32BitsToSingle(integer);
            AreEqual(flt32, result32);

        }
        

        [Test]
        public void CodeGenPatternStoreLoad() {
            
            var myInstance = new SampleClass();
            // --- store
            long[]  src = { 0, 1, 2, 3, 1, 5, 6 };
            
            myInstance.field0 =            src[0];
            myInstance.field1 = (int)      src[1];
            myInstance.field2 = (short)    src[2];
            myInstance.field3 = (byte)     src[3];
            
            myInstance.field4 =            src[4] != 0;
            
            myInstance.field5 = BitConverter.Int64BitsToDouble (     src[5]);
            myInstance.field6 = BitConverter.Int32BitsToSingle ((int)src[6]);

            // --- load
            long[] dst = new long[7];

            dst[0] = myInstance.field0;
            dst[1] = myInstance.field1;
            dst[2] = myInstance.field2;
            dst[3] = myInstance.field3;
            
            dst[4] = myInstance.field4 ? 1 : 0;

            dst[5] = BitConverter.DoubleToInt64Bits(myInstance.field5);
            dst[6] = BitConverter.SingleToInt32Bits(myInstance.field6);

            AreEqual(src, dst);
        }
    }
    
    // --- Following code snippet can be copy- / pasted into https://www.linqpad.net/ to see generated IL
    public class SampleClass {
        public long     field0;
        public int      field1;
        public short    field2;
        public byte     field3;
        public bool     field4;
        public double   field5;
        public float    field6;
            
        private void LoadInstance(long[] dst, SampleClass src) {
            dst[0] = src.field0;
            dst[1] = src.field1;
            dst[2] = src.field2;
            dst[3] = src.field3;
            
            dst[4] = src.field4 ? 1 : 0;

            dst[5] = BitConverter.DoubleToInt64Bits(src.field5);
            dst[6] = BitConverter.SingleToInt32Bits(src.field6);
        }
        
        [Test]
        public void LoadWithExpressionPattern () {
            long[] dst = new long[7];
            var src = new SampleClass() {field0 = 10, field1 = 11, field2 = 12, field3 = 13, field4 = true, field5 = 11.5, field6 = 22.5f };
            LoadInstance(dst, src);
        }
    }
    // --- end of snippet



}

#endif
