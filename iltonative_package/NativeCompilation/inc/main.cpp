// testCPP.cpp : Defines the entry point for the console application.
//

#include "common.h"

#define USE_MRT 0

#include "gcenv.h"

#if !USE_MRT
#include "gc.h"
#include "objecthandle.h"
#include "gcdesc.h"
#else
extern "C" Object * RhNewObject(MethodTable * pMT);
extern "C" Object * RhNewArray(MethodTable * pMT, int32_t elements);
extern "C" void RhpReversePInvoke2(ReversePInvokeFrame* pRevFrame);
extern "C" void RhpReversePInvokeReturn(ReversePInvokeFrame* pRevFrame);
extern "C" int32_t RhpEnableConservativeStackReporting();
extern "C" void RhpRegisterSimpleModule(SimpleModuleHeader* pModule);
#endif // USE_MRT

int __initialize_runtime()
{
#if USE_MRT
    RhpEnableConservativeStackReporting();
#else
    //
    // Initialize system info
    //
    InitializeSystemInfo();

    // 
    // Initialize free object methodtable. The GC uses a special array-like methodtable as placeholder
    // for collected free space.
    //
    static MethodTable freeObjectMT;
    freeObjectMT.InitializeFreeObject();
    g_pFreeObjectMethodTable = &freeObjectMT;

    //
    // Initialize handle table
    //
    if (!Ref_Initialize())
        return -1;

    //
    // Initialize GC heap
    //
    GCHeap *pGCHeap = GCHeap::CreateGCHeap();
    if (!pGCHeap)
        return -1;

    if (FAILED(pGCHeap->Initialize()))
        return -1;

    //
    // Initialize current thread
    //
    ThreadStore::AttachCurrentThread(false);
#endif

    return 0;
}

void __shutdown_runtime()
{
}

void __reverse_pinvoke(ReversePInvokeFrame* pRevFrame)
{
#if USE_MRT
    RhpReversePInvoke2(pRevFrame);
#endif // USE_MRT
}

void __reverse_pinvoke_return(ReversePInvokeFrame* pRevFrame)
{
#if USE_MRT
    RhpReversePInvokeReturn(pRevFrame);
#endif // USE_MRT
}

void __register_module(SimpleModuleHeader* pModule)
{
#if USE_MRT
    RhpRegisterSimpleModule(pModule);
#endif // USE_MRT
}

namespace System { class Object {
public:
    EEType * get_EEType() { return *(EEType **)this; }
}; };

namespace System { class Array : public System::Object {
public:
    int32_t GetArrayLength() {
        return *(int32_t *)((void **)this + 1);
    }
    void * GetArrayData() {
        return (void **)this + 2;
    }
}; };

namespace System { class String : public System::Object { public:
static MethodTable * __getMethodTable();
}; };



//
// The fast paths for object allocation and write barriers is performance critical. They are often
// hand written in assembly code, etc.
//
Object * __allocate_object(MethodTable * pMT)
{
#if !USE_MRT
    alloc_context * acontext = GetThread()->GetAllocContext();
    Object * pObject;

    size_t size = pMT->GetBaseSize();

    BYTE* result = acontext->alloc_ptr;
    BYTE* advance = result + size;
    if (advance <= acontext->alloc_limit)
    {
        acontext->alloc_ptr = advance;
        pObject = (Object *)result;
    }
    else
    {
        pObject = GCHeap::GetGCHeap()->Alloc(acontext, size, 0);
        if (pObject == NULL)
            return NULL; // TODO: Throw OOM
    }

    pObject->SetMethodTable(pMT);

    return pObject;
#else
    return RhNewObject(pMT);
#endif
}

Object * __allocate_string(int32_t len)
{
#if !USE_MRT
    alloc_context * acontext = GetThread()->GetAllocContext();
    Object * pObject;

    // TODO: Overflow checks
    size_t size = 2 * sizeof(intptr_t) + sizeof(int32_t) + 2 * len;
    // Align up
    size = (size + (sizeof(intptr_t) - 1)) & ~(sizeof(intptr_t) - 1);

    BYTE* result = acontext->alloc_ptr;
    BYTE* advance = result + size;
    if (advance <= acontext->alloc_limit)
    {
        acontext->alloc_ptr = advance;
        pObject = (Object *)result;
    }
    else
    {
        pObject = GCHeap::GetGCHeap()->Alloc(acontext, size, 0);
        if (pObject == NULL)
            return NULL; // TODO: Throw OOM
    }

    pObject->SetMethodTable(System::String::__getMethodTable());

    *(int32_t *)(((intptr_t *)pObject)+1) = len;

    return pObject;
#else
    return RhNewArray(System::String::__getMethodTable(), len);
#endif
}

Object * __allocate_array(MethodTable * pMT, size_t elements)
{
#if !USE_MRT
    alloc_context * acontext = GetThread()->GetAllocContext();
    Object * pObject;

    // TODO: Overflow checks
    size_t size = 3 * sizeof(intptr_t) + (elements * pMT->RawGetComponentSize());
    // Align up
    size = (size + (sizeof(intptr_t) - 1)) & ~(sizeof(intptr_t) - 1);

    BYTE* result = acontext->alloc_ptr;
    BYTE* advance = result + size;
    if (advance <= acontext->alloc_limit)
    {
        acontext->alloc_ptr = advance;
        pObject = (Object *)result;
    }
    else
    {
        pObject = GCHeap::GetGCHeap()->Alloc(acontext, size, 0);
        if (pObject == NULL)
            return NULL; // TODO: Throw OOM
    }

    pObject->SetMethodTable(pMT);

    *(int32_t *)(((intptr_t *)pObject)+1) = (int32_t)elements;

    return pObject;
#else
    return RhNewArray(pMT, (int32_t)elements); // TODO: type mismatch
#endif 
}

void __throw_exception(void * pEx)
{
    // TODO: Exception throwing
    throw pEx;
}

Object * __load_string_literal(const char * string)
{
    // TODO: Cache/intern string literals
    // TODO: Unicode string literals

    size_t len = strlen(string);

    Object * pString = __allocate_string((int32_t)len);

    uint16_t * p = (uint16_t *)((char*)pString + sizeof(intptr_t) + sizeof(int32_t));
    for (size_t i = 0; i < len; i++)
        p[i] = string[i];
    return pString;
}

// TODO: Rewrite in C#

Object * __castclass_class(void * p, MethodTable * pTargetMT)
{
    Object * o = (Object *)p;

    if (o == NULL)
        return o;

    MethodTable * pMT = o->RawGetMethodTable();

    do {
        if (pMT == pTargetMT)
            return o;

        pMT = pMT->GetParent();
    } while (pMT);

    // TODO: Handle corner cases, throw proper exception
    throw 1;
}

Object * __isinst_class(void * p, MethodTable * pTargetMT)
{
    Object * o = (Object *)p;

    if (o == NULL)
        return o;

    MethodTable * pMT = o->RawGetMethodTable();

    do {
        if (pMT == pTargetMT)
            return o;

        pMT = pMT->GetParent();
    } while (pMT);

    // TODO: Handle corner cases
    return NULL;
}

__declspec(noreturn)
__declspec(noinline)
void ThrowRangeOverflowException()
{
    throw 0;
}

void __range_check(void * a, size_t elem)
{
    if (elem >= *((size_t*)a + 1))
        ThrowRangeOverflowException();
}

namespace System { class String__Array : public System::Object { public:
static MethodTable * __getMethodTable();
}; };


Object * __get_commandline_args(int argc, char * argv[])
{       
    System::Array * p = (System::Array *)__allocate_array(System::String__Array::__getMethodTable(), argc);

    for (int i = 0; i < argc; i++)
    {
        // TODO: Write barrier
        ((Object **)(p->GetArrayData()))[i] = __load_string_literal(argv[i]);
    }    

    return (Object *)p;
}

// FCalls

namespace System { namespace Runtime { class RuntimeImports : public System::Object { public:
static uint8_t TryArrayCopy(System::Array*, int32_t, System::Array*, int32_t, int32_t);
static uint8_t TryArrayClear(System::Array*, int32_t, int32_t);
}; }; };

FORCEINLINE bool CheckArraySlice(System::Array * pArray, int32_t index, int32_t length)
{
    int32_t arrayLength = pArray->GetArrayLength();

    return (0 <= index) && (index <= arrayLength) &&
        (0 <= length) && (length <= arrayLength) &&
        (length <= arrayLength - index);
}

uint8_t System::Runtime::RuntimeImports::TryArrayCopy(System::Array* pSourceArray, int32_t sourceIndex, System::Array* pDestinationArray, int32_t destinationIndex, int32_t length)
{
    if (pSourceArray == NULL || pDestinationArray == NULL)
        return false;

    EEType* pArrayType = pSourceArray->get_EEType();
    EEType* pDestinationArrayType = pDestinationArray->get_EEType();

    if (pArrayType != pDestinationArrayType)
    {
        // TODO: Type safety check
        return false;
#if false
        if (!pArrayType->IsEquivalentTo(pDestinationArrayType))
            return false;
#endif
    }

    if (!pArrayType->HasComponentSize())
        return false; // an array

    size_t componentSize = pArrayType->RawGetComponentSize();

    if (!CheckArraySlice(pSourceArray, sourceIndex, length))
        return false;

    if (!CheckArraySlice(pDestinationArray, destinationIndex, length))
        return false;

    if (length == 0)
        return true;

    uint8_t * pSourceData = (uint8_t *)pSourceArray->GetArrayData() + sourceIndex * componentSize;
    uint8_t * pDestinationData = (uint8_t *)pDestinationArray->GetArrayData() + destinationIndex * componentSize;
    size_t size = length * componentSize;

    // TODO: Write barrier
#if false
    if (pArrayType->HasReferenceFields())
    {
        if (pDestinationData <= pSourceData || pSourceData + size <= pDestinationData)
            ForwardGCSafeCopy(pDestinationData, pSourceData, size);
        else
            BackwardGCSafeCopy(pDestinationData, pSourceData, size);

        RhpBulkWriteBarrier(pDestinationData, (UInt32)size);
    }
    else
#endif
    {
        memmove(pDestinationData, pSourceData, size);
    }

    return true;
}

void GCSafeFillMemory(void * mem, size_t size, size_t pv)
{
    uint8_t * memBytes = (uint8_t *)mem;
    uint8_t * endBytes = &memBytes[size];

    // handle unaligned bytes at the beginning 
    while (!IS_ALIGNED(memBytes, sizeof(void *)) && (memBytes < endBytes))
        *memBytes++ = (uint8_t)pv;

    // now write pointer sized pieces 
    size_t nPtrs = (endBytes - memBytes) / sizeof(void *);
    UIntNative* memPtr = (UIntNative*)memBytes;
    for (size_t i = 0; i < nPtrs; i++)
        *memPtr++ = pv;

    // handle remaining bytes at the end 
    memBytes = (uint8_t*)memPtr;
    while (memBytes < endBytes)
        *memBytes++ = (uint8_t)pv;
}

uint8_t System::Runtime::RuntimeImports::TryArrayClear(System::Array *pArray, int32_t index, int32_t length)
{
    if (pArray == NULL)
        return false;

    EEType* pArrayType = pArray->get_EEType();

    size_t componentSize = pArrayType->RawGetComponentSize();
    if (componentSize == 0) // Not an array
        return false;

    if (!CheckArraySlice(pArray, index, length))
        return false;

    if (length == 0)
        return true;

    GCSafeFillMemory((uint8_t *)pArray->GetArrayData() + index * componentSize, length * componentSize, 0);

    return true;
}