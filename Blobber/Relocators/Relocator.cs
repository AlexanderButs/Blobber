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
        /// Relocates the type reference.
        /// </summary>
        /// <param name="typeRef">The type reference.</param>
        /// <returns></returns>
        protected abstract TypeSig TryRelocateTypeRef(TypeRef typeRef);

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

        #region TryRelocate*

        /// <summary>
        /// Relocates the specified type.
        /// </summary>
        /// <param name="typeSig">The type sig.</param>
        /// <returns></returns>
        private TypeSig TryRelocateTypeSig(TypeSig typeSig)
        {
            if (typeSig == null)
                return null;

            if (typeSig is CorLibTypeSig)
                return null;

            if (typeSig is GenericInstSig)
                return TryRelocateGeneric((GenericInstSig)typeSig);

            if (typeSig is PtrSig)
                return null;

            if (typeSig is ByRefSig)
                return TryRelocateByRef((ByRefSig)typeSig);

            if (typeSig is ArraySig)
                return TryRelocateArray((ArraySig)typeSig);

            if (typeSig is SZArraySig)
                return TryRelocateSZArray((SZArraySig)typeSig);

            if (typeSig is GenericVar)
                return null; // TODO constraints

            if (typeSig is GenericMVar)
                return null; // TODO constraints

            if (typeSig is ValueTypeSig || typeSig is ClassSig)
            {
                var typeRef = typeSig.TryGetTypeRef();
                if (typeRef != null)
                    return TryRelocateTypeRef(typeRef);
                var typeDefOrRef = TryRelocateTypeDefOrRef(typeSig.ToTypeDefOrRef());
                return typeDefOrRef?.ToTypeSig();
            }

            throw new InvalidOperationException();
        }

        private TypeSig TryRelocateByRef(ByRefSig byRefSig)
        {
            var innerTypeSig = TryRelocateTypeSig(byRefSig.Next);
            if (innerTypeSig == null)
                return null;

            return new ByRefSig(innerTypeSig);
        }

        private TypeSig TryRelocateGeneric(GenericInstSig genericInstSig)
        {
            bool relocated = false;
            var genericTypeSig = TryRelocateTypeSig(genericInstSig.GenericType) as ClassOrValueTypeSig;
            if (genericTypeSig != null)
            {
                genericInstSig.GenericType = genericTypeSig;
                relocated = true;
            }

            for (int genericParameterIndex = 0; genericParameterIndex < genericInstSig.GenericArguments.Count; genericParameterIndex++)
            {
                var genericParameterType = TryRelocateTypeSig(genericInstSig.GenericArguments[genericParameterIndex]);
                if (genericParameterType != null)
                {
                    genericInstSig.GenericArguments[genericParameterIndex] = genericParameterType;
                    relocated = true;
                }
            }

            return relocated ? genericInstSig : null;
        }

        private TypeSig TryRelocateArray(ArraySig arraySig)
        {
            var nextType = TryRelocateTypeSig(arraySig.Next);
            if (nextType == null)
                return null;
            return new ArraySig(nextType);
        }

        private TypeSig TryRelocateSZArray(SZArraySig szArraySig)
        {
            var nextType = TryRelocateTypeSig(szArraySig.Next);
            if (nextType == null)
                return null;
            return new SZArraySig(nextType);
        }

        private ITypeDefOrRef TryRelocateTypeDefOrRef(ITypeDefOrRef typeDefOrRef)
        {
            if (typeDefOrRef == null)
                return null;

            // no need to relocate
            var typeDef = typeDefOrRef as TypeDef;
            if (typeDef != null)
                return null;

            var typeRef = typeDefOrRef as TypeRef;
            if (typeRef != null)
                return TryRelocateTypeRef(typeRef).ToTypeDefOrRef();

            var typeSpec = typeDefOrRef as TypeSpec;
            if (typeSpec != null)
                return TryRelocateTypeSig(typeSpec.TypeSig).ToTypeDefOrRef();

            throw new NotImplementedException();
        }

        #endregion


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
                foreach (var instruction in methodDef.Body.Instructions)
                    Relocate(instruction);
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
            bool replaced = RelocateType<Instruction>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<IList<Instruction>>(ref instruction.Operand, RelocateOperand)
                            || RelocateType<MemberRef>(ref instruction.Operand, RelocateOperand)
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
            var newClass = TryRelocateTypeDefOrRef(operand.DeclaringType);
            if (newClass != null)
                operand.Class = newClass;
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
