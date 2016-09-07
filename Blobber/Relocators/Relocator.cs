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

    internal abstract class Relocator
    {
        private readonly ModuleDefMD2 _targetModule;

        protected Relocator(ModuleDefMD2 targetModule)
        {
            _targetModule = targetModule;
        }

        /// <summary>
        /// Relocates the specified type definition or reference.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        protected abstract TypeDef RelocateType(IType type);

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

        private ITypeDefOrRef Relocate(ITypeDefOrRef typeDefOrRef)
        {
            if (typeDefOrRef == null)
                return null;

            var typeDef = typeDefOrRef as TypeDef;
            if (typeDef != null)
                return RelocateType(typeDef);

            var typeRef = typeDefOrRef as TypeRef;
            if (typeRef != null)
                return RelocateType(typeRef);

            var typeSpec = typeDefOrRef as TypeSpec;
            if (typeSpec != null)
                return Relocate(typeSpec.TypeSig).ToTypeDefOrRef();

            throw new NotImplementedException();
        }


        private void RelocateBase(TypeDef typeDef)
        {
            var baseTypeDefOrRef = (ITypeDefOrRef)_targetModule.Import(typeDef.BaseType);
            if (baseTypeDefOrRef != null)
            {
                var newBaseType = Relocate(typeDef.BaseType);
                if (newBaseType != null)
                    typeDef.BaseType = newBaseType;
            }

            foreach (var interfaceType in typeDef.Interfaces)
            {
                var newInterfaceType = RelocateType(interfaceType.Interface);
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

            var propertyType = Relocate(propertyDef.PropertySig.RetType);
            if (propertyType != null)
                propertyDef.PropertySig.RetType = propertyType;

            for (int indexIndex = 0; indexIndex < propertyDef.PropertySig.Params.Count; indexIndex++)
            {
                var indexType = Relocate(propertyDef.PropertySig.Params[indexIndex]);
                if (indexType != null)
                    propertyDef.PropertySig.Params[indexIndex] = indexType;
            }
        }

        private void Relocate(FieldDef fieldDef)
        {
            var typeSig = Relocate(fieldDef.FieldType);
            if (typeSig != null)
                fieldDef.FieldType = typeSig;
        }

        private void Relocate(MethodDef methodDef)
        {
            if (methodDef.HasBody)
            {
                foreach (var variable in methodDef.Body.Variables)
                    Relocate(variable);
                foreach (var instruction in methodDef.Body.Instructions)
                    Relocate(instruction);
            }
            foreach (var parameter in methodDef.Parameters)
                Relocate(parameter);

            var returnType = Relocate(methodDef.ReturnType);
            if (returnType != null)
                methodDef.ReturnType = returnType;
        }

        /// <summary>
        /// Relocates the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void Relocate(Parameter parameter)
        {
            var typeSig = Relocate(parameter.Type);
            if (typeSig != null)
                parameter.Type = typeSig;
        }

        /// <summary>
        /// Relocates the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        private void Relocate(Local variable)
        {
            var typeSig = Relocate(variable.Type);
            if (typeSig != null)
                variable.Type = typeSig;
        }

        /// <summary>
        /// Relocates the specified type.
        /// </summary>
        /// <param name="typeSig">The type sig.</param>
        /// <returns></returns>
        private TypeSig Relocate(TypeSig typeSig)
        {
            if (typeSig == null)
                return null;

            if (typeSig.IsSZArray)
                return RelocateSZArray(typeSig);

            if (typeSig.IsArray)
                return RelocateArray(typeSig);

            if (typeSig.IsGenericInstanceType)
                return RelocateGeneric(typeSig);

            return RelocateType(typeSig)?.ToTypeSig();
        }

        private TypeSig RelocateGeneric(TypeSig typeSig)
        {
            bool relocated = false;
            var genericInstSig = (GenericInstSig)typeSig;
            var genericTypeSig = Relocate(genericInstSig.GenericType) as ClassOrValueTypeSig;
            if (genericTypeSig != null)
            {
                genericInstSig.GenericType = genericTypeSig;
                relocated = true;
            }

            for (int genericParameterIndex = 0; genericParameterIndex < genericInstSig.GenericArguments.Count; genericParameterIndex++)
            {
                var genericParameterType = Relocate(genericInstSig.GenericArguments[genericParameterIndex]);
                if (genericParameterType != null)
                {
                    genericInstSig.GenericArguments[genericParameterIndex] = genericParameterType;
                    relocated = true;
                }
            }

            return relocated ? genericInstSig : null;
        }

        private TypeSig RelocateArray(TypeSig typeSig)
        {
            var nextType = Relocate(typeSig.Next);
            if (nextType == null)
                return null;
            return new ArraySig(nextType);
        }

        private TypeSig RelocateSZArray(TypeSig typeSig)
        {
            var nextType = Relocate(typeSig.Next);
            if (nextType == null)
                return null;
            return new SZArraySig(nextType);
        }

        private bool Relocate(Instruction instruction)
        {
            if (instruction.Operand == null)
                return false;
            bool replaced = RelocateType<Instruction>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<IList<Instruction>>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MemberRef>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<TypeRef>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<ITypeDefOrRef>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<IField>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MethodDef>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MethodSpec>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<IMethod>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<ITokenOperand>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MethodSig>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<Parameter>(ref instruction.Operand, RelocateOperand);
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
            var newClass = RelocateType(operand.DeclaringType);
            if (newClass != null)
                operand.Class = newClass;
            return operand;
        }

        private TypeRef RelocateOperand(TypeRef operand)
        {
            var typeSig = operand.ToTypeSig();
            var newTypeSig = RelocateType(typeSig);
            if (newTypeSig != null)
                operand = _targetModule.Import(newTypeSig);
            return operand;
        }

        private ITypeDefOrRef RelocateOperand(ITypeDefOrRef operand)
        {
            return Relocate(operand);
        }

        private IField RelocateOperand(IField operand)
        {
            var fieldType = RelocateType(operand.FieldSig.Type)?.ToTypeSig();
            if (fieldType != null)
                operand.FieldSig.Type = fieldType;
            return operand;
        }

        private MethodDef RelocateOperand(MethodDef operand)
        {
            return operand;
        }

        private MethodSpec RelocateOperand(MethodSpec operand)
        {
            var declaringType = RelocateType(operand.DeclaringType);
            if (declaringType != null)
                operand.Method = declaringType.FindMethod(operand.Method.Name, operand.Method.MethodSig);

            if (operand.GenericInstMethodSig != null)
            {
                for (int genericParameterIndex = 0; genericParameterIndex < operand.GenericInstMethodSig.GenericArguments.Count; genericParameterIndex++)
                {
                    var genericParameterType = Relocate(operand.GenericInstMethodSig.GenericArguments[genericParameterIndex]);
                    if (genericParameterType != null)
                        operand.GenericInstMethodSig.GenericArguments[genericParameterIndex] = genericParameterType;
                }

            }
            return operand;
        }

        private IMethod RelocateOperand(IMethod operand)
        {
            return operand;
        }

        private ITokenOperand RelocateOperand(ITokenOperand operand)
        {
            return operand;
        }

        private MethodSig RelocateOperand(MethodSig operand)
        {
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
