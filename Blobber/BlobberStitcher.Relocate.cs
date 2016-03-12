#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    using System;
    using System.Collections.Generic;
    using dnlib.DotNet;
    using dnlib.DotNet.Emit;
    using Relocators;
    using Utility;

    partial class BlobberStitcher
    {
        /// <summary>
        /// Relocates the specified target module.
        /// </summary>
        /// <param name="targetModule">The target module.</param>
        /// <param name="relocator">The relocator.</param>
        private void Relocate(ModuleDefMD2 targetModule, IRelocator relocator)
        {
            foreach (var typeDef in targetModule.GetTypes())
                Relocate(typeDef, relocator);
        }

        private void Relocate(TypeDef typeDef, IRelocator relocator)
        {
            foreach (var methodDef in typeDef.Methods)
                Relocate(methodDef, relocator);
            foreach (var propertyDef in typeDef.Properties)
                Relocate(propertyDef, relocator);
            foreach (var fieldDef in typeDef.Fields)
                Relocate(fieldDef, relocator);
        }

        private void Relocate(PropertyDef propertyDef, IRelocator relocator)
        {
            var getMethod = propertyDef.GetMethod;
            if (getMethod != null)
                Relocate(getMethod, relocator);
            var setMethod = propertyDef.SetMethod;
            if (setMethod != null)
                Relocate(setMethod, relocator);

            var propertyType = Relocate(propertyDef.PropertySig.RetType, relocator);
            if (propertyType != null)
                propertyDef.PropertySig.RetType = propertyType;

            for (int indexIndex = 0; indexIndex < propertyDef.PropertySig.Params.Count; indexIndex++)
            {
                var indexType = Relocate(propertyDef.PropertySig.Params[indexIndex], relocator);
                if (indexType != null)
                    propertyDef.PropertySig.Params[indexIndex] = indexType;
            }
        }

        private void Relocate(FieldDef fieldDef, IRelocator relocator)
        {
            var typeSig = Relocate(fieldDef.FieldType, relocator);
            if (typeSig != null)
                fieldDef.FieldType = typeSig;
        }

        private void Relocate(MethodDef methodDef, IRelocator relocator)
        {
            if (methodDef.HasBody)
            {
                foreach (var variable in methodDef.Body.Variables)
                    Relocate(variable, relocator);
                foreach (var instruction in methodDef.Body.Instructions)
                    Relocate(instruction, relocator);
            }
            foreach (var parameter in methodDef.Parameters)
                Relocate(parameter, relocator);

            var returnType = Relocate(methodDef.ReturnType, relocator);
            if (returnType != null)
                methodDef.ReturnType = returnType;
        }

        /// <summary>
        /// Relocates the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="relocator">The relocator.</param>
        private void Relocate(Parameter parameter, IRelocator relocator)
        {
            var typeSig = Relocate(parameter.Type, relocator);
            if (typeSig != null)
                parameter.Type = typeSig;
        }

        /// <summary>
        /// Relocates the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="relocator">The relocator.</param>
        private void Relocate(Local variable, IRelocator relocator)
        {
            var typeSig = Relocate(variable.Type, relocator);
            if (typeSig != null)
                variable.Type = typeSig;
        }

        /// <summary>
        /// Relocates the specified type.
        /// </summary>
        /// <param name="typeSig">The type sig.</param>
        /// <param name="relocator">The relocator.</param>
        /// <returns></returns>
        private TypeSig Relocate(TypeSig typeSig, IRelocator relocator)
        {
            if (typeSig == null)
                return null;

            if (typeSig.IsSZArray)
                return RelocateSZArray(typeSig, relocator);

            if (typeSig.IsArray)
                return RelocateArray(typeSig, relocator);

            if (typeSig.IsGenericInstanceType)
                return RelocateGeneric(typeSig, relocator);

            return relocator.Relocate(typeSig)?.ToTypeSig();
        }

        private TypeSig RelocateGeneric(TypeSig typeSig, IRelocator relocator)
        {
            bool relocated = false;
            var genericInstSig = (GenericInstSig)typeSig;
            var genericTypeSig = Relocate(genericInstSig.GenericType, relocator) as ClassOrValueTypeSig;
            if (genericTypeSig != null)
            {
                genericInstSig.GenericType = genericTypeSig;
                relocated = true;
            }

            for (int genericParameterIndex = 0; genericParameterIndex < genericInstSig.GenericArguments.Count; genericParameterIndex++)
            {
                var genericParameterType = Relocate(genericInstSig.GenericArguments[genericParameterIndex], relocator);
                if (genericParameterType != null)
                {
                    genericInstSig.GenericArguments[genericParameterIndex] = genericParameterType;
                    relocated = true;
                }
            }

            return relocated ? genericInstSig : null;
        }

        private TypeSig RelocateArray(TypeSig typeSig, IRelocator relocator)
        {
            var nextType = Relocate(typeSig.Next, relocator);
            if (nextType == null)
                return null;
            return new ArraySig(nextType);
        }

        private TypeSig RelocateSZArray(TypeSig typeSig, IRelocator relocator)
        {
            var nextType = Relocate(typeSig.Next, relocator);
            if (nextType == null)
                return null;
            return new SZArraySig(nextType);
        }

        private bool Relocate(Instruction instruction, IRelocator relocator)
        {
            if (instruction.Operand == null)
                return false;
            bool replaced = RelocateType<Instruction>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<IList<Instruction>>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<MemberRef>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<TypeRef>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<ITypeDefOrRef>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<IField>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<MethodDef>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<MethodSpec>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<IMethod>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<ITokenOperand>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<MethodSig>(ref instruction.Operand, relocator, RelocateOperand)
                            || RelocateType<Parameter>(ref instruction.Operand, relocator, RelocateOperand);
            return replaced;
        }

        private Instruction RelocateOperand(Instruction operand, IRelocator relocator)
        {
            Relocate(operand, relocator);
            return operand;
        }

        private IList<Instruction> RelocateOperand(IList<Instruction> operands, IRelocator relocator)
        {
            foreach (var operand in operands)
                Relocate(operand, relocator);
            return operands;
        }

        private MemberRef RelocateOperand(MemberRef operand, IRelocator relocator)
        {
            var newClass = relocator.Relocate(operand.DeclaringType);
            if (newClass != null)
                operand.Class = newClass;
            return operand;
        }

        private TypeRef RelocateOperand(TypeRef operand, IRelocator relocator)
        {
            var typeSig = operand.ToTypeSig();
            var newTypeSig = relocator.Relocate(typeSig);
            if (newTypeSig != null)
                operand = newTypeSig.Module.Import(newTypeSig);
            return operand;
        }

        private ITypeDefOrRef RelocateOperand(ITypeDefOrRef operand, IRelocator relocator)
        {
            return operand;
        }

        private IField RelocateOperand(IField operand, IRelocator relocator)
        {
            var fieldType = relocator.Relocate(operand.FieldSig.Type)?.ToTypeSig();
            if (fieldType != null)
                operand.FieldSig.Type = fieldType;
            return operand;
        }

        private MethodDef RelocateOperand(MethodDef operand, IRelocator relocator)
        {
            return operand;
        }

        private MethodSpec RelocateOperand(MethodSpec operand, IRelocator relocator)
        {
            var declaringType = relocator.Relocate(operand.DeclaringType);
            if (declaringType != null)
                operand.Method = declaringType.FindMethod(operand.Method.Name, operand.Method.MethodSig);

            if (operand.GenericInstMethodSig != null)
            {
                for (int genericParameterIndex = 0; genericParameterIndex < operand.GenericInstMethodSig.GenericArguments.Count; genericParameterIndex++)
                {
                    var genericParameterType = Relocate(operand.GenericInstMethodSig.GenericArguments[genericParameterIndex], relocator);
                    if (genericParameterType != null)
                        operand.GenericInstMethodSig.GenericArguments[genericParameterIndex] = genericParameterType;
                }

            }
            return operand;
        }

        private IMethod RelocateOperand(IMethod operand, IRelocator relocator)
        {
            return operand;
        }

        private ITokenOperand RelocateOperand(ITokenOperand operand, IRelocator relocator)
        {
            return operand;
        }

        private MethodSig RelocateOperand(MethodSig operand, IRelocator relocator)
        {
            return operand;
        }

        private Parameter RelocateOperand(Parameter operand, IRelocator relocator)
        {
            return operand;
        }

        private static bool RelocateType<TOperand>(ref object operand, IRelocator relocator, Func<TOperand, IRelocator, TOperand> typedRelocator)
            where TOperand : class
        {
            var typedOperand = operand as TOperand;
            if (typedOperand == null)
                return false;
            operand = typedRelocator(typedOperand, relocator);
            return true;
        }
    }
}
