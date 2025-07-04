#ifndef COPY_INITIAL_MESH_DATA_HLSL_
#define COPY_INITIAL_MESH_DATA_HLSL_

/////////////////////////////////////////////////////////////////////////////////

ByteAddressBuffer meshVertexData;
ByteAddressBuffer meshBonesPerVertexData;
RWByteAddressBuffer outInitialDeformedMeshData;

int inputVertexSizeInBytes;
int outDataVertexOffset;
uint totalMeshVertices;
int outBonesWeightsDataOffset;
int inputBonesWeightsDataOffset;

/////////////////////////////////////////////////////////////////////////////////

SourceSkinnedMeshVertex ReadSourceVertex(int vertexIndex)
{
    int vertexByteOffset = vertexIndex * inputVertexSizeInBytes;
    SourceSkinnedMeshVertex rv = (SourceSkinnedMeshVertex)0;
#ifndef RUKHANKA_INPLACE_SKINNING
    CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_COPY_MESH_DATA, vertexByteOffset, 40, meshVertexData);

    float4 v0 = asfloat(meshVertexData.Load4(vertexByteOffset + 0));
    float4 v1 = asfloat(meshVertexData.Load4(vertexByteOffset + 16));
    float1 v2 = asfloat(meshVertexData.Load(vertexByteOffset + 32));
    rv.position = v0.xyz;
    rv.normal = float3(v0.w, v1.xy);
    rv.tangent = float3(v1.zw, v2.x);
#endif

    int baseBoneWeightIndex = inputBonesWeightsDataOffset + vertexIndex;

    CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_COPY_MESH_DATA, baseBoneWeightIndex * 4, 4, meshBonesPerVertexData);
    uint boneWeightsOffsetAndCountPacked = meshBonesPerVertexData.Load(baseBoneWeightIndex * 4);
    rv.boneWeightsOffset = SourceSkinnedMeshVertex::GetBoneWeightsOffsetFromPackedUINT(boneWeightsOffsetAndCountPacked) + outBonesWeightsDataOffset;
    rv.boneWeightsCount = SourceSkinnedMeshVertex::GetBoneWeightsCountFromPackedUINT(boneWeightsOffsetAndCountPacked);

    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

[numthreads(128, 1, 1)]
void CopyInitialMeshData(uint tid: SV_DispatchThreadID)
{
    if (tid >= totalMeshVertices)
        return;

    SourceSkinnedMeshVertex v = ReadSourceVertex(tid);

    int outVertexOffset = tid + outDataVertexOffset;
    v.WriteIntoRawBuffer(outInitialDeformedMeshData, outVertexOffset);
}

#endif // COPY_INITIAL_MESH_DATA_HLSL_
