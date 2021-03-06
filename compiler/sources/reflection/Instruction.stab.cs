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
using java.lang;
using org.objectweb.asm;

namespace stab.reflection {
    
    public abstract class Instruction {
        final static Instruction Aaload = new SimpleInstruction(Opcode.Aaload);
        final static Instruction Aastore = new SimpleInstruction(Opcode.Aastore);
        final static Instruction Aconst_Null = new SimpleInstruction(Opcode.Aconst_Null);
        final static Instruction Areturn = new SimpleInstruction(Opcode.Areturn);
        final static Instruction Arraylength = new SimpleInstruction(Opcode.Arraylength);
        final static Instruction Athrow = new SimpleInstruction(Opcode.Athrow);
        final static Instruction Baload = new SimpleInstruction(Opcode.Baload);
        final static Instruction Bastore = new SimpleInstruction(Opcode.Bastore);
        final static Instruction Caload = new SimpleInstruction(Opcode.Caload);
        final static Instruction Castore = new SimpleInstruction(Opcode.Castore);
        final static Instruction D2f = new SimpleInstruction(Opcode.D2f);
        final static Instruction D2i = new SimpleInstruction(Opcode.D2i);
        final static Instruction D2l = new SimpleInstruction(Opcode.D2l);
        final static Instruction Dadd = new SimpleInstruction(Opcode.Dadd);
        final static Instruction Daload = new SimpleInstruction(Opcode.Daload);
        final static Instruction Dastore = new SimpleInstruction(Opcode.Dastore);
        final static Instruction Dcmpg = new SimpleInstruction(Opcode.Dcmpg);
        final static Instruction Dcmpl = new SimpleInstruction(Opcode.Dcmpl);
        final static Instruction Dconst_0 = new SimpleInstruction(Opcode.Dconst_0);
        final static Instruction Dconst_1 = new SimpleInstruction(Opcode.Dconst_1);
        final static Instruction Ddiv = new SimpleInstruction(Opcode.Ddiv);
        final static Instruction Dmul = new SimpleInstruction(Opcode.Dmul);
        final static Instruction Dneg = new SimpleInstruction(Opcode.Dneg);
        final static Instruction Drem = new SimpleInstruction(Opcode.Drem);
        final static Instruction Dreturn = new SimpleInstruction(Opcode.Dreturn);
        final static Instruction Dsub = new SimpleInstruction(Opcode.Dsub);
        final static Instruction Dup = new SimpleInstruction(Opcode.Dup);
        final static Instruction Dup_X1 = new SimpleInstruction(Opcode.Dup_X1);
        final static Instruction Dup_X2 = new SimpleInstruction(Opcode.Dup_X2);
        final static Instruction Dup2 = new SimpleInstruction(Opcode.Dup2);
        final static Instruction Dup2_X1 = new SimpleInstruction(Opcode.Dup2_X1);
        final static Instruction Dup2_X2 = new SimpleInstruction(Opcode.Dup2_X2);
        final static Instruction F2d = new SimpleInstruction(Opcode.F2d);
        final static Instruction F2i = new SimpleInstruction(Opcode.F2i);
        final static Instruction F2l = new SimpleInstruction(Opcode.F2l);
        final static Instruction Fadd = new SimpleInstruction(Opcode.Fadd);
        final static Instruction Faload = new SimpleInstruction(Opcode.Faload);
        final static Instruction Fastore = new SimpleInstruction(Opcode.Fastore);
        final static Instruction Fcmpg = new SimpleInstruction(Opcode.Fcmpg);
        final static Instruction Fcmpl = new SimpleInstruction(Opcode.Fcmpl);
        final static Instruction Fconst_0 = new SimpleInstruction(Opcode.Fconst_0);
        final static Instruction Fconst_1 = new SimpleInstruction(Opcode.Fconst_1);
        final static Instruction Fconst_2 = new SimpleInstruction(Opcode.Fconst_2);
        final static Instruction Fdiv = new SimpleInstruction(Opcode.Fdiv);
        final static Instruction Fmul = new SimpleInstruction(Opcode.Fmul);
        final static Instruction Fneg = new SimpleInstruction(Opcode.Fneg);
        final static Instruction Frem = new SimpleInstruction(Opcode.Frem);
        final static Instruction Freturn = new SimpleInstruction(Opcode.Freturn);
        final static Instruction Fsub = new SimpleInstruction(Opcode.Fsub);
        final static Instruction I2b = new SimpleInstruction(Opcode.I2b);
        final static Instruction I2c = new SimpleInstruction(Opcode.I2c);
        final static Instruction I2d = new SimpleInstruction(Opcode.I2d);
        final static Instruction I2f = new SimpleInstruction(Opcode.I2f);
        final static Instruction I2l = new SimpleInstruction(Opcode.I2l);
        final static Instruction I2s = new SimpleInstruction(Opcode.I2s);
        final static Instruction Iadd = new SimpleInstruction(Opcode.Iadd);
        final static Instruction Iaload = new SimpleInstruction(Opcode.Iaload);
        final static Instruction Iand = new SimpleInstruction(Opcode.Iand);
        final static Instruction Iastore = new SimpleInstruction(Opcode.Iastore);
        final static Instruction Iconst_0 = new SimpleInstruction(Opcode.Iconst_0);
        final static Instruction Iconst_1 = new SimpleInstruction(Opcode.Iconst_1);
        final static Instruction Iconst_2 = new SimpleInstruction(Opcode.Iconst_2);
        final static Instruction Iconst_3 = new SimpleInstruction(Opcode.Iconst_3);
        final static Instruction Iconst_4 = new SimpleInstruction(Opcode.Iconst_4);
        final static Instruction Iconst_5 = new SimpleInstruction(Opcode.Iconst_5);
        final static Instruction Iconst_M1 = new SimpleInstruction(Opcode.Iconst_M1);
        final static Instruction Idiv = new SimpleInstruction(Opcode.Idiv);
        final static Instruction Imul = new SimpleInstruction(Opcode.Imul);
        final static Instruction Ineg = new SimpleInstruction(Opcode.Ineg);
        final static Instruction Ior = new SimpleInstruction(Opcode.Ior);
        final static Instruction Irem = new SimpleInstruction(Opcode.Irem);
        final static Instruction Ireturn = new SimpleInstruction(Opcode.Ireturn);
        final static Instruction Ishl = new SimpleInstruction(Opcode.Ishl);
        final static Instruction Ishr = new SimpleInstruction(Opcode.Ishr);
        final static Instruction Isub = new SimpleInstruction(Opcode.Isub);
        final static Instruction Iushr = new SimpleInstruction(Opcode.Iushr);
        final static Instruction Ixor = new SimpleInstruction(Opcode.Ixor);
        final static Instruction L2d = new SimpleInstruction(Opcode.L2d);
        final static Instruction L2f = new SimpleInstruction(Opcode.L2f);
        final static Instruction L2i = new SimpleInstruction(Opcode.L2i);
        final static Instruction Ladd = new SimpleInstruction(Opcode.Ladd);
        final static Instruction Laload = new SimpleInstruction(Opcode.Laload);
        final static Instruction Land = new SimpleInstruction(Opcode.Land);
        final static Instruction Lastore = new SimpleInstruction(Opcode.Lastore);
        final static Instruction Lcmp = new SimpleInstruction(Opcode.Lcmp);
        final static Instruction Lconst_0 = new SimpleInstruction(Opcode.Lconst_0);
        final static Instruction Lconst_1 = new SimpleInstruction(Opcode.Lconst_1);
        final static Instruction Ldiv = new SimpleInstruction(Opcode.Ldiv);
        final static Instruction Lmul = new SimpleInstruction(Opcode.Lmul);
        final static Instruction Lneg = new SimpleInstruction(Opcode.Lneg);
        final static Instruction Lor = new SimpleInstruction(Opcode.Lor);
        final static Instruction Lrem = new SimpleInstruction(Opcode.Lrem);
        final static Instruction Lreturn = new SimpleInstruction(Opcode.Lreturn);
        final static Instruction Lshl = new SimpleInstruction(Opcode.Lshl);
        final static Instruction Lshr = new SimpleInstruction(Opcode.Lshr);
        final static Instruction Lsub = new SimpleInstruction(Opcode.Lsub);
        final static Instruction Lushr = new SimpleInstruction(Opcode.Lushr);
        final static Instruction Lxor = new SimpleInstruction(Opcode.Lxor);
        final static Instruction Monitorenter = new SimpleInstruction(Opcode.Monitorenter);
        final static Instruction Monitorexit = new SimpleInstruction(Opcode.Monitorexit);
        final static Instruction Nop = new SimpleInstruction(Opcode.Nop);
        final static Instruction Pop = new SimpleInstruction(Opcode.Pop);
        final static Instruction Pop2 = new SimpleInstruction(Opcode.Pop2);
        final static Instruction Return = new SimpleInstruction(Opcode.Return);
        final static Instruction Saload = new SimpleInstruction(Opcode.Saload);
        final static Instruction Sastore = new SimpleInstruction(Opcode.Sastore);
        final static Instruction Swap = new SimpleInstruction(Opcode.Swap);

        final static Instruction Aload_0 = new LocalVariableInstruction(Opcode.Aload, 0);
        final static Instruction Aload_1 = new LocalVariableInstruction(Opcode.Aload, 1);
        final static Instruction Aload_2 = new LocalVariableInstruction(Opcode.Aload, 2);
        final static Instruction Aload_3 = new LocalVariableInstruction(Opcode.Aload, 3);
        final static Instruction Fload_0 = new LocalVariableInstruction(Opcode.Fload, 0);
        final static Instruction Fload_1 = new LocalVariableInstruction(Opcode.Fload, 1);
        final static Instruction Fload_2 = new LocalVariableInstruction(Opcode.Fload, 2);
        final static Instruction Fload_3 = new LocalVariableInstruction(Opcode.Fload, 3);
        final static Instruction Dload_0 = new LocalVariableInstruction(Opcode.Dload, 0);
        final static Instruction Dload_1 = new LocalVariableInstruction(Opcode.Dload, 1);
        final static Instruction Dload_2 = new LocalVariableInstruction(Opcode.Dload, 2);
        final static Instruction Dload_3 = new LocalVariableInstruction(Opcode.Dload, 3);
        final static Instruction Iload_0 = new LocalVariableInstruction(Opcode.Iload, 0);
        final static Instruction Iload_1 = new LocalVariableInstruction(Opcode.Iload, 1);
        final static Instruction Iload_2 = new LocalVariableInstruction(Opcode.Iload, 2);
        final static Instruction Iload_3 = new LocalVariableInstruction(Opcode.Iload, 3);
        final static Instruction Lload_0 = new LocalVariableInstruction(Opcode.Lload, 0);
        final static Instruction Lload_1 = new LocalVariableInstruction(Opcode.Lload, 1);
        final static Instruction Lload_2 = new LocalVariableInstruction(Opcode.Lload, 2);
        final static Instruction Lload_3 = new LocalVariableInstruction(Opcode.Lload, 3);

        final static Instruction Astore_0 = new LocalVariableInstruction(Opcode.Astore, 0);
        final static Instruction Astore_1 = new LocalVariableInstruction(Opcode.Astore, 1);
        final static Instruction Astore_2 = new LocalVariableInstruction(Opcode.Astore, 2);
        final static Instruction Astore_3 = new LocalVariableInstruction(Opcode.Astore, 3);
        final static Instruction Fstore_0 = new LocalVariableInstruction(Opcode.Fstore, 0);
        final static Instruction Fstore_1 = new LocalVariableInstruction(Opcode.Fstore, 1);
        final static Instruction Fstore_2 = new LocalVariableInstruction(Opcode.Fstore, 2);
        final static Instruction Fstore_3 = new LocalVariableInstruction(Opcode.Fstore, 3);
        final static Instruction Dstore_0 = new LocalVariableInstruction(Opcode.Dstore, 0);
        final static Instruction Dstore_1 = new LocalVariableInstruction(Opcode.Dstore, 1);
        final static Instruction Dstore_2 = new LocalVariableInstruction(Opcode.Dstore, 2);
        final static Instruction Dstore_3 = new LocalVariableInstruction(Opcode.Dstore, 3);
        final static Instruction Istore_0 = new LocalVariableInstruction(Opcode.Istore, 0);
        final static Instruction Istore_1 = new LocalVariableInstruction(Opcode.Istore, 1);
        final static Instruction Istore_2 = new LocalVariableInstruction(Opcode.Istore, 2);
        final static Instruction Istore_3 = new LocalVariableInstruction(Opcode.Istore, 3);
        final static Instruction Lstore_0 = new LocalVariableInstruction(Opcode.Lstore, 0);
        final static Instruction Lstore_1 = new LocalVariableInstruction(Opcode.Lstore, 1);
        final static Instruction Lstore_2 = new LocalVariableInstruction(Opcode.Lstore, 2);
        final static Instruction Lstore_3 = new LocalVariableInstruction(Opcode.Lstore, 3);

        final static Instruction Newarray_4 = new IntOperandInstruction(Opcode.Newarray, 4);
        final static Instruction Newarray_5 = new IntOperandInstruction(Opcode.Newarray, 5);
        final static Instruction Newarray_6 = new IntOperandInstruction(Opcode.Newarray, 6);
        final static Instruction Newarray_7 = new IntOperandInstruction(Opcode.Newarray, 7);
        final static Instruction Newarray_8 = new IntOperandInstruction(Opcode.Newarray, 8);
        final static Instruction Newarray_9 = new IntOperandInstruction(Opcode.Newarray, 9);
        final static Instruction Newarray_10 = new IntOperandInstruction(Opcode.Newarray, 10);
        final static Instruction Newarray_11 = new IntOperandInstruction(Opcode.Newarray, 11);

        protected Instruction(Opcode opcode) {
            this.Opcode = opcode;
        }
        
        public Opcode Opcode^;

        public virtual int LocalVariable {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual int IntOperand {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual int Increment {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual LabelMarker LabelMarker {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual Object ConstantValue {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual FieldInfo Field {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual MethodInfo Method {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual TypeInfo Type {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual int MinimumKey {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        public virtual int MaximumKey {
            get {
                throw new UnsupportedOperationException();
            }
        }

        public virtual LabelMarker[] Labels {
            get {
                throw new UnsupportedOperationException();
            }
        }

        public virtual LabelMarker DefaultLabel {
            get {
                throw new UnsupportedOperationException();
            }
        }

        public virtual int[] Keys {
            get {
                throw new UnsupportedOperationException();
            }
        }

        public virtual int Dimensions {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        virtual Label Label {
            get {
                throw new UnsupportedOperationException();
            }
        }
        
        abstract void accept(MethodVisitor visitor);
    }
    
    public class LabelMarker : Instruction {
        private Label label;
        
        LabelMarker(Label label)
            : super(Opcode.LabelMarker) {
            this.label = label;
        }
        
        override Label Label {
            get {
                return label;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitLabel(label);
        }
    }
    
    class SimpleInstruction : Instruction {
        SimpleInstruction(Opcode opcode)
            : super(opcode) {
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitInsn(this.Opcode.Value);
        }
    }

    class LocalVariableInstruction : Instruction {
        private int localVariable;
    
        LocalVariableInstruction(Opcode opcode, int localVariable)
            : super(opcode) {
            this.localVariable = localVariable;
        }
        
        public override int LocalVariable {
            get {
                return localVariable;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitVarInsn(this.Opcode.Value, localVariable);
        }
    }
    
    class IntOperandInstruction : Instruction {
        private int intOperand;
        
        IntOperandInstruction(Opcode opcode, int intOperand)
            : super(opcode) {
            this.intOperand = intOperand;
        }
        
        public override int IntOperand {
            get {
                return intOperand;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitIntInsn(this.Opcode.Value, intOperand);
        }
    }
    
    class IncrementInstruction : Instruction {
        private int localVariable;
        private int increment;
        
        IncrementInstruction(int localVariable, int increment)
            : super(Opcode.Iinc) {
            this.localVariable = localVariable;
            this.increment = increment;
        }
        
        public override int LocalVariable {
            get {
                return localVariable;
            }
        }
        
        public override int Increment {
            get {
                return increment;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitIincInsn(localVariable, increment);
        }
    }

    class JumpInstruction : Instruction {
        private LabelMarker labelMarker;
        
        JumpInstruction(Opcode opcode, LabelMarker labelMarker)
            : super(opcode) {
            this.labelMarker = labelMarker;
        }
        
        public override LabelMarker LabelMarker {
            get {
                return labelMarker;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitJumpInsn(this.Opcode.Value, labelMarker.Label);
        }
    }
    
    class LoadConstantInstruction : Instruction {
        private Object constantValue;
        
        LoadConstantInstruction(Object constantValue)
            : super(Opcode.Ldc) {
            this.constantValue = constantValue;
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitLdcInsn(constantValue);
        }
    }
    
    class FieldInstruction : Instruction {
        private FieldInfo field;
        
        FieldInstruction(Opcode opcode, FieldInfo field)
            : super(opcode) {
            this.field = field;
        }
        
        public override FieldInfo Field {
            get {
                return field;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitFieldInsn(this.Opcode.Value, field.DeclaringType.FullName, field.Name, field.Descriptor);
        }
    }
    
    class MethodInstruction : Instruction {
        private MethodInfo method;
        
        MethodInstruction(Opcode opcode, MethodInfo method)
            : super(opcode) {
            this.method = method;
        }
        
        public override MethodInfo Method {
            get {
                return method;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitMethodInsn(this.Opcode.Value, method.DeclaringType.FullName, method.Name, method.Descriptor);
        }
    }
    
    class TypeInstruction : Instruction {
        private TypeInfo type;
        
        TypeInstruction(Opcode opcode, TypeInfo type)
            : super(opcode) {
            this.type = type;
        }
        
        public override TypeInfo Type {
            get {
                return type;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            if (type.IsGenericParameter) {
                var desc = type.Descriptor;
                visitor.visitTypeInsn(this.Opcode.Value, desc.substring(1, desc.length() - 1));
            } else {
                visitor.visitTypeInsn(this.Opcode.Value, type.FullName);
            }
        }
    }
    
    class LookupSwitchInstruction : Instruction {
        private int[] keys;
        private LabelMarker defaultLabel;
        private LabelMarker[] labels;
    
        LookupSwitchInstruction(int[] keys, LabelMarker[] labels, LabelMarker defaultLabel)
            : super(Opcode.Lookupswitch) {
            this.keys = keys;
            this.labels = labels;
            this.defaultLabel = defaultLabel;
        }
        
        public override int[] Keys {
            get {
                return keys;
            }
        }
        
        public override LabelMarker[] Labels {
            get {
                return labels;
            }
        }
        
        public override LabelMarker DefaultLabel {
            get {
                return defaultLabel;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            var t = new Label[sizeof(labels)];
            for (int i = 0; i < sizeof(t); i++) {
                t[i] = labels[i].Label;
            }
            visitor.visitLookupSwitchInsn(defaultLabel.Label, keys, t);
        }
    }
    
    class TableSwitchInstruction : Instruction {
        private int minimumKey;
        private int maximumKey;
        private LabelMarker[] labels;
        private LabelMarker defaultLabel;
        
        TableSwitchInstruction(int minimumKey, int maximumKey, LabelMarker[] labels, LabelMarker defaultLabel)
            : super(Opcode.Tableswitch) {
            this.minimumKey = minimumKey;
            this.maximumKey = maximumKey;
            this.labels = labels;
            this.defaultLabel = defaultLabel;
        }
        
        public override int MinimumKey {
            get {
                return minimumKey;
            }
        }
        
        public override int MaximumKey {
            get {
                return maximumKey;
            }
        }
        
        public override LabelMarker[] Labels {
            get {
                return labels;
            }
        }
        
        public override LabelMarker DefaultLabel {
            get {
                return defaultLabel;
            }
        }
        
        override void accept(MethodVisitor visitor) {
            var t = new Label[sizeof(labels)];
            for (int i = 0; i < sizeof(t); i++) {
                t[i] = labels[i].Label;
            }
            visitor.visitTableSwitchInsn(minimumKey, maximumKey, defaultLabel.Label, t);
        }
    }
    
    class MultiNewArrayInstruction : Instruction {
        private TypeInfo type;
        private int dimensions;
        
        MultiNewArrayInstruction(TypeInfo type, int dimensions)
            : super(Opcode.Multianewarray) {
            this.type = type;
            this.dimensions = dimensions;
        }
        
        override void accept(MethodVisitor visitor) {
            visitor.visitMultiANewArrayInsn(type.FullName, dimensions);
        }
    }
}
