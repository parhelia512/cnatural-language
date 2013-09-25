/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */
using org.objectweb.asm;

namespace stab.reflection {

    public enum Opcode {
        Aaload(Opcodes.AALOAD),
        Aastore(Opcodes.AASTORE),
        Aconst_Null(Opcodes.ACONST_NULL),
        Aload(Opcodes.ALOAD),
        Anewarray(Opcodes.ANEWARRAY),
        Areturn(Opcodes.ARETURN),
        Arraylength(Opcodes.ARRAYLENGTH),
        Astore(Opcodes.ASTORE),
        Athrow(Opcodes.ATHROW),
        Baload(Opcodes.BALOAD),
        Bastore(Opcodes.BASTORE),
        Bipush(Opcodes.BIPUSH),
        Caload(Opcodes.CALOAD),
        Castore(Opcodes.CASTORE),
        Checkcast(Opcodes.CHECKCAST),
        D2f(Opcodes.D2F),
        D2i(Opcodes.D2I),
        D2l(Opcodes.D2L),
        Dadd(Opcodes.DADD),
        Daload(Opcodes.DALOAD),
        Dastore(Opcodes.DASTORE),
        Dcmpg(Opcodes.DCMPG),
        Dcmpl(Opcodes.DCMPL),
        Dconst_0(Opcodes.DCONST_0),
        Dconst_1(Opcodes.DCONST_1),
        Ddiv(Opcodes.DDIV),
        Dload(Opcodes.DLOAD),
        Dmul(Opcodes.DMUL),
        Dneg(Opcodes.DNEG),
        Drem(Opcodes.DREM),
        Dreturn(Opcodes.DRETURN),
        Dstore(Opcodes.DSTORE),
        Dsub(Opcodes.DSUB),
        Dup(Opcodes.DUP),
        Dup_X1(Opcodes.DUP_X1),
        Dup_X2(Opcodes.DUP_X2),
        Dup2(Opcodes.DUP2),
        Dup2_X1(Opcodes.DUP2_X1),
        Dup2_X2(Opcodes.DUP2_X2),
        F2d(Opcodes.F2D),
        F2i(Opcodes.F2I),
        F2l(Opcodes.F2L),
        Fadd(Opcodes.FADD),
        Faload(Opcodes.FALOAD),
        Fastore(Opcodes.FASTORE),
        Fcmpg(Opcodes.FCMPG),
        Fcmpl(Opcodes.FCMPL),
        Fconst_0(Opcodes.FCONST_0),
        Fconst_1(Opcodes.FCONST_1),
        Fconst_2(Opcodes.FCONST_2),
        Fdiv(Opcodes.FDIV),
        Fload(Opcodes.FLOAD),
        Fmul(Opcodes.FMUL),
        Fneg(Opcodes.FNEG),
        Frem(Opcodes.FREM),
        Freturn(Opcodes.FRETURN),
        Fstore(Opcodes.FSTORE),
        Fsub(Opcodes.FSUB),
        Getfield(Opcodes.GETFIELD),
        Getstatic(Opcodes.GETSTATIC),
        Goto(Opcodes.GOTO),
        I2b(Opcodes.I2B),
        I2c(Opcodes.I2C),
        I2d(Opcodes.I2D),
        I2f(Opcodes.I2F),
        I2l(Opcodes.I2L),
        I2s(Opcodes.I2S),
        Iadd(Opcodes.IADD),
        Iaload(Opcodes.IALOAD),
        Iand(Opcodes.IAND),
        Iastore(Opcodes.IASTORE),
        Iconst_0(Opcodes.ICONST_0),
        Iconst_1(Opcodes.ICONST_1),
        Iconst_2(Opcodes.ICONST_2),
        Iconst_3(Opcodes.ICONST_3),
        Iconst_4(Opcodes.ICONST_4),
        Iconst_5(Opcodes.ICONST_5),
        Iconst_M1(Opcodes.ICONST_M1),
        Idiv(Opcodes.IDIV),
        If_acmpeq(Opcodes.IF_ACMPEQ),
        If_acmpne(Opcodes.IF_ACMPNE),
        If_icmpeq(Opcodes.IF_ICMPEQ),
        If_icmpge(Opcodes.IF_ICMPGE),
        If_icmpgt(Opcodes.IF_ICMPGT),
        If_icmple(Opcodes.IF_ICMPLE),
        If_icmplt(Opcodes.IF_ICMPLT),
        If_icmpne(Opcodes.IF_ICMPNE),
        Ifeq(Opcodes.IFEQ),
        Ifge(Opcodes.IFGE),
        Ifgt(Opcodes.IFGT),
        Ifle(Opcodes.IFLE),
        Iflt(Opcodes.IFLT),
        Ifne(Opcodes.IFNE),
        Ifnull(Opcodes.IFNULL),
        Ifnonnull(Opcodes.IFNONNULL),
        Iinc(Opcodes.IINC),
        Iload(Opcodes.ILOAD),
        Imul(Opcodes.IMUL),
        Ineg(Opcodes.INEG),
        Instanceof(Opcodes.INSTANCEOF),
        Invokevirtual(Opcodes.INVOKEVIRTUAL),
        Invokespecial(Opcodes.INVOKESPECIAL),
        Invokestatic(Opcodes.INVOKESTATIC),
        Invokeinterface(Opcodes.INVOKEINTERFACE),
        Invokedynamic(Opcodes.INVOKEDYNAMIC),
        Ior(Opcodes.IOR),
        Irem(Opcodes.IREM),
        Ireturn(Opcodes.IRETURN),
        Ishl(Opcodes.ISHL),
        Ishr(Opcodes.ISHR),
        Istore(Opcodes.ISTORE),
        Isub(Opcodes.ISUB),
        Iushr(Opcodes.IUSHR),
        Ixor(Opcodes.IXOR),
        Jsr(Opcodes.JSR),
        L2d(Opcodes.L2D),
        L2f(Opcodes.L2F),
        L2i(Opcodes.L2I),
        LabelMarker(-1),
        Ladd(Opcodes.LADD),
        Laload(Opcodes.LALOAD),
        Land(Opcodes.LAND),
        Lastore(Opcodes.LASTORE),
        Lcmp(Opcodes.LCMP),
        Lconst_0(Opcodes.LCONST_0),
        Lconst_1(Opcodes.LCONST_1),
        Ldc(Opcodes.LDC),
        Ldiv(Opcodes.LDIV),
        Lload(Opcodes.LLOAD),
        Lmul(Opcodes.LMUL),
        Lneg(Opcodes.LNEG),
        Lookupswitch(Opcodes.LOOKUPSWITCH),
        Lor(Opcodes.LOR),
        Lrem(Opcodes.LREM),
        Lreturn(Opcodes.LRETURN),
        Lshl(Opcodes.LSHL),
        Lshr(Opcodes.LSHR),
        Lstore(Opcodes.LSTORE),
        Lsub(Opcodes.LSUB),
        Lushr(Opcodes.LUSHR),
        Lxor(Opcodes.LXOR),
        Monitorenter(Opcodes.MONITORENTER),
        Monitorexit(Opcodes.MONITOREXIT),
        Multianewarray(Opcodes.MULTIANEWARRAY),
        New(Opcodes.NEW),
        Newarray(Opcodes.NEWARRAY),
        Nop(Opcodes.NOP),
        Pop(Opcodes.POP),
        Pop2(Opcodes.POP2),
        Putfield(Opcodes.PUTFIELD),
        Putstatic(Opcodes.PUTSTATIC),
        Ret(Opcodes.RET),
        Return(Opcodes.RETURN),
        Saload(Opcodes.SALOAD),
        Sastore(Opcodes.SASTORE),
        Sipush(Opcodes.SIPUSH),
        Swap(Opcodes.SWAP),
        Tableswitch(Opcodes.TABLESWITCH);
        
        private int value;
        
        private Opcode(int value) {
            this.value = value;
        }
        
        public int Value {
            get {
                return value;
            }
        }
    }

}