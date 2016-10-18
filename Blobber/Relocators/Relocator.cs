#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber.Relocators
{
    using System;
    using System.Collections.Generic;
    using dnlib.DotNet;
    using dnlib.DotNet.Emit;
    using StitcherBoy.Reflection;

    internal abstract class Relocator: TypeRelocator
    {
        private readonly ModuleDefMD2 _targetModule;

        protected Relocator(ModuleDefMD2 targetModule)
        {
            _targetModule = targetModule;
        }

        /// <summary>
        /// Relocates the specified target module.
        /// </summary>
        public void Relocate()
        {
            foreach (var typeDef in _targetModule.GetTypes())
                Relocate(typeDef);
        }

        private void Relocate(TypeDef typeDef)
        {
            RelocateBase(typeDef);
            foreach (var methodDef in typeDef.Methods)
                Relocate(methodDef);
            foreach (var propertyDef in typeDef.Properties)
                Relocate(propertyDef);
            foreach (var fieldDef in typeDef.Fields)
                Relocate(fieldDef);
        }

        private void RelocateBase(TypeDef typeDef)
        {
            var baseTypeDefOrRef = (ITypeDefOrRef)_targetModule.Import(typeDef.BaseType);
            if (baseTypeDefOrRef != null)
            {
                var newBaseType = TryRelocateTypeDefOrRef(typeDef.BaseType);
                if (newBaseType != null)
                    typeDef.BaseType = newBaseType;
            }

            foreach (var interfaceType in typeDef.Interfaces)
            {
                var newInterfaceType = TryRelocateTypeDefOrRef(interfaceType.Interface);
                if (newInterfaceType != null)
                    interfaceType.Interface = newInterfaceType;
            }
        }

        private void Relocate(PropertyDef propertyDef)
        {
            var getMethod = propertyDef.GetMethod;
            if (getMethod != null)
                Relocate(getMethod);
            var setMethod = propertyDef.SetMethod;
            if (setMethod != null)
                Relocate(setMethod);

            var propertyType = TryRelocateTypeSig(propertyDef.PropertySig.RetType);
            if (propertyType != null)
                propertyDef.PropertySig.RetType = propertyType;

            for (int indexIndex = 0; indexIndex < propertyDef.PropertySig.Params.Count; indexIndex++)
            {
                var indexType = TryRelocateTypeSig(propertyDef.PropertySig.Params[indexIndex]);
                if (indexType != null)
                    propertyDef.PropertySig.Params[indexIndex] = indexType;
            }
        }

        private void Relocate(FieldDef fieldDef)
        {
            var typeSig = TryRelocateTypeSig(fieldDef.FieldType);
            if (typeSig != null)
                fieldDef.FieldType = typeSig;
        }

        private void Relocate(MethodDef methodDef)
        {
            if (methodDef.HasBody)
            {
                foreach (var variable in methodDef.Body.Variables)
                    Relocate(variable);
                for (int instructionIndex = 0; instructionIndex < methodDef.Body.Instructions.Count; instructionIndex++)
                {
                    var instruction = methodDef.Body.Instructions[instructionIndex];
                    Relocate(instruction);
                }
            }
            foreach (var parameter in methodDef.Parameters)
                Relocate(parameter);

            var returnType = TryRelocateTypeSig(methodDef.ReturnType);
            if (returnType != null)
                methodDef.ReturnType = returnType;
        }

        /// <summary>
        /// Relocates the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void Relocate(Parameter parameter)
        {
            var typeSig = TryRelocateTypeSig(parameter.Type);
            if (typeSig != null)
                parameter.Type = typeSig;
        }

        /// <summary>
        /// Relocates the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        private void Relocate(Local variable)
        {
            var typeSig = TryRelocateTypeSig(variable.Type);
            if (typeSig != null)
                variable.Type = typeSig;
        }

        private bool Relocate(Instruction instruction)
        {
            if (instruction.Operand == null)
                return false;
            // relocations are from most to least specific
            bool replaced = RelocateType<IList<Instruction>>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<Instruction>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MemberRef>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<ITypeDefOrRef>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<IField>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MethodDef>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MethodSpec>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MethodSig>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<Parameter>(ref instruction.Operand, RelocateOperand)
                            ;
            return replaced;
        }

        private Instruction RelocateOperand(Instruction operand)
        {
            Relocate(operand);
            return operand;
        }

        private IList<Instruction> RelocateOperand(IList<Instruction> operands)
        {
            foreach (var operand in operands)
                Relocate(operand);
            return operands;
        }

        private MemberRef RelocateOperand(MemberRef operand)
        {
            var newClass = TryRelocateTypeDefOrRef(operand.DeclaringType);
            if (newClass != null)
                operand.Class = newClass;

            if (operand.MethodSig != null)
            {
                var newMethodSig = RelocateOperand(operand.MethodSig);
                if (newMethodSig != null)
                    operand.MethodSig = newMethodSig;
            }

            if (operand.FieldSig != null)
            {
                var newFieldSig = TryRelocateTypeSig(operand.FieldSig.Type);
                if (newFieldSig != null)
                    operand.FieldSig.Type = newFieldSig;
            }

            return operand;
        }

        private ITypeDefOrRef RelocateOperand(ITypeDefOrRef operand)
        {
            var relocated = TryRelocateTypeDefOrRef(operand);
            return relocated ?? operand;
        }

        private IField RelocateOperand(IField operand)
        {
            var fieldType = TryRelocateTypeSig(operand.FieldSig.Type);
            if (fieldType != null)
                operand.FieldSig.Type = fieldType;
            return operand;
        }

        private MethodDef RelocateOperand(MethodDef operand)
        {
            foreach (var parameter in operand.Parameters)
            {
                var parameterType = TryRelocateTypeSig(parameter.Type);
                if (parameterType != null)
                    parameter.Type = parameterType;
            }

            return operand;
        }

        private MethodSpec RelocateOperand(MethodSpec operand)
        {
            var declaringType = TryRelocateTypeDefOrRef(operand.DeclaringType);
            if (declaringType != null)
            {
                var declaringTypeDef = _targetModule.Find(declaringType);
                operand.Method = declaringTypeDef.FindMethod(operand.Method.Name, operand.Method.MethodSig);
            }

            if (operand.GenericInstMethodSig != null)
            {
                for (int genericParameterIndex = 0; genericParameterIndex < operand.GenericInstMethodSig.GenericArguments.Count; genericParameterIndex++)
                {
                    var genericParameterType = TryRelocateTypeSig(operand.GenericInstMethodSig.GenericArguments[genericParameterIndex]);
                    if (genericParameterType != null)
                        operand.GenericInstMethodSig.GenericArguments[genericParameterIndex] = genericParameterType;
                }

            }
            return operand;
        }

        private MethodSig RelocateOperand(MethodSig operand)
        {
            var returnType = TryRelocateTypeSig(operand.RetType);
            if (returnType != null)
                operand.RetType = returnType;

            for (int parameterIndex = 0; parameterIndex < operand.Params.Count; parameterIndex++)
            {
                var parameterType = TryRelocateTypeSig(operand.Params[parameterIndex]);
                if (parameterType != null)
                    operand.Params[parameterIndex] = parameterType;
            }

            return operand;
        }

        private Parameter RelocateOperand(Parameter operand)
        {
            return operand;
        }

        private static bool RelocateType<TOperand>(ref object operand, Func<TOperand, TOperand> typedRelocator)
            where TOperand : class
        {
            var typedOperand = operand as TOperand;
            if (typedOperand == null)
                return false;
            operand = typedRelocator(typedOperand);
            return true;
        }
    }
}
