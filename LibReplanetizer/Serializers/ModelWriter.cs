﻿using LibReplanetizer.LevelObjects;
using LibReplanetizer.Models;
using LibReplanetizer.Models.Animations;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace LibReplanetizer
{
    public static class ModelWriter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void WriteIqe(string fileName, Level level, Model model)
        {
            Logger.Trace(fileName);

            string filePath = Path.GetDirectoryName(fileName);

            if (!(model is MobyModel mobyModel)) return;

            using (StreamWriter spookyStream = new StreamWriter(fileName))
            {
                spookyStream.WriteLine("# Inter-Quake Export");

                // Binding pose
                for (int i = 0; i < mobyModel.boneDatas.Count; i++)
                {
                    Quaternion quat = mobyModel.boneMatrices[i].mat1.ExtractRotation();

                    Matrix4 mat = mobyModel.boneMatrices[i].mat1;
                    float x = mat.M41 / 1024f;
                    float y = mat.M42 / 1024f;
                    float z = mat.M43 / 1024f;

                    float xx = mobyModel.boneDatas[i].unk1 / 1024f;
                    float yy = mobyModel.boneDatas[i].unk2 / 1024f;
                    float zz = mobyModel.boneDatas[i].unk3 / 1024f;

                    short par = (short)(mobyModel.boneMatrices[i].bb / 0x40);
                    spookyStream.WriteLine("joint h" + i.ToString() + " " + (par == 0 ? "" : par.ToString()));
                    spookyStream.WriteLine("pq " + xx.ToString() + " " + yy.ToString() + " " + zz.ToString());
                }

                List<Animation> anims;

                if (mobyModel.id == 0)
                    anims = level.playerAnimations;
                else
                    anims = mobyModel.animations;

                int idx = 0;
                int animIndex = 0;
                foreach (Animation anim in anims)
                {
                    if (anim.frames.Count == 0) continue;
                    spookyStream.WriteLine("animation " + animIndex.ToString());
                    spookyStream.WriteLine("framerate " + 60f * anim.speed);

                    int frameIndex = 0;
                    foreach (Frame frame in anim.frames)
                    {
                        idx = 0;
                        spookyStream.WriteLine("frame " + frameIndex.ToString());
                        foreach (short[] quat in frame.rotations)
                        {
                            BoneData bd = mobyModel.boneDatas[idx];
                            //Vector3 vec = mat.mat1.ExtractTranslation();

                            /*
                            foreach(short[] tran in frame.translations)
                            {
                                if(tran[3] / 0x100 == idx)
                                {
                                    x *= -tran[0] / 32767f;
                                    y *= -tran[1] / 32767f;
                                    z *= -tran[2] / 32767f;
                                }
                            }*/

                            float xx = mobyModel.boneDatas[idx].unk1 / 1024f;
                            float yy = mobyModel.boneDatas[idx].unk2 / 1024f;
                            float zz = mobyModel.boneDatas[idx].unk3 / 1024f;

                            spookyStream.WriteLine("pq " + xx.ToString() + " " + yy.ToString() + " " + zz.ToString() + " " + quat[0] / 32767f + " " + quat[1] / 32767f + " " + quat[2] / 32767f + " " + -quat[3] / 32767f);
                            idx++;
                        }
                        frameIndex++;
                    }
                    animIndex++;
                }


                //Faces
                int tCnt = 0;
                for (int i = 0; i < model.indexBuffer.Length / 3; i++)
                {
                    if (model.textureConfig != null && tCnt < model.textureConfig.Count)
                    {
                        if (i * 3 >= model.textureConfig[tCnt].start)
                        {
                            spookyStream.WriteLine("mesh " + model.textureConfig[tCnt].ID.ToString(""));
                            if (model.textureConfig[tCnt].ID != -1)
                            {
                                spookyStream.WriteLine("material " + model.textureConfig[tCnt].ID.ToString("x") + ".png");
                                Bitmap bump = level.textures[model.textureConfig[tCnt].ID].getTextureImage();
                                bump.Save(filePath + "/" + model.textureConfig[tCnt].ID.ToString("x") + ".png");
                            }
                            tCnt++;
                        }
                    }
                    int f1 = model.indexBuffer[i * 3 + 0];
                    int f2 = model.indexBuffer[i * 3 + 1];
                    int f3 = model.indexBuffer[i * 3 + 2];
                    spookyStream.WriteLine("fm " + f1 + " " + f2 + " " + f3);
                }

                //Vertices, normals, UV's
                for (int x = 0; x < model.vertexBuffer.Length / 8; x++)
                {
                    float px = model.vertexBuffer[(x * 0x08) + 0x0];
                    float py = model.vertexBuffer[(x * 0x08) + 0x1];
                    float pz = model.vertexBuffer[(x * 0x08) + 0x2];
                    float nx = model.vertexBuffer[(x * 0x08) + 0x3];
                    float ny = model.vertexBuffer[(x * 0x08) + 0x4];
                    float nz = model.vertexBuffer[(x * 0x08) + 0x5];
                    float tu = model.vertexBuffer[(x * 0x08) + 0x6];
                    float tv = model.vertexBuffer[(x * 0x08) + 0x7];
                    spookyStream.WriteLine("vp " + px.ToString("G") + " " + py.ToString("G") + " " + pz.ToString("G"));
                    spookyStream.WriteLine("vn " + nx.ToString("G") + " " + ny.ToString("G") + " " + nz.ToString("G"));
                    spookyStream.WriteLine("vt " + tu.ToString("G") + " " + tv.ToString("G"));

                    byte[] weights = BitConverter.GetBytes(model.weights[x]);
                    byte[] indices = BitConverter.GetBytes(model.ids[x]);

                    spookyStream.WriteLine("vb " + indices[3].ToString() + " " + (weights[3] / 255f).ToString() + " " + indices[2].ToString() + " " + (weights[2] / 255f).ToString() + " " + indices[1].ToString() + " " + (weights[1] / 255f).ToString() + " " + indices[0].ToString() + " " + (weights[0] / 255f).ToString());
                }
            }
        }

        public static void WriteObj(string fileName, Model model)
        {
            string pathName = Path.GetDirectoryName(fileName);
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);

            using (StreamWriter MTLfs = new StreamWriter(pathName + "\\" + fileNameNoExtension + ".mtl"))
            {
                // List used mtls to prevent it from making duplicate entries
                List<int> usedMtls = new List<int>();

                for (int i = 0; i < model.textureConfig.Count; i++)
                {
                    int modelTextureID = model.textureConfig[0].ID;
                    if (!usedMtls.Contains(modelTextureID))
                    {
                        MTLfs.WriteLine("newmtl mtl_" + modelTextureID);
                        MTLfs.WriteLine("Ns 1000");
                        MTLfs.WriteLine("Ka 1.000000 1.000000 1.000000");
                        MTLfs.WriteLine("Kd 1.000000 1.000000 1.000000");
                        MTLfs.WriteLine("Ni 1.000000");
                        MTLfs.WriteLine("d 1.000000");
                        MTLfs.WriteLine("illum 1");
                        MTLfs.WriteLine("map_Kd tex_" + model.textureConfig[i].ID + ".png");
                        usedMtls.Add(modelTextureID);
                    }
                }
            }

            using (StreamWriter OBJfs = new StreamWriter(fileName))
            {
                OBJfs.WriteLine("o Object_" + model.id.ToString("X4"));
                if (model.textureConfig != null)
                    OBJfs.WriteLine("mtllib " + fileNameNoExtension + ".mtl");
                //Vertices, normals, UV's
                for (int x = 0; x < model.vertexBuffer.Length / 8; x++)
                {
                    float px = model.vertexBuffer[(x * 0x08) + 0x0];
                    float py = model.vertexBuffer[(x * 0x08) + 0x1];
                    float pz = model.vertexBuffer[(x * 0x08) + 0x2];
                    float nx = model.vertexBuffer[(x * 0x08) + 0x3];
                    float ny = model.vertexBuffer[(x * 0x08) + 0x4];
                    float nz = model.vertexBuffer[(x * 0x08) + 0x5];
                    float tu = model.vertexBuffer[(x * 0x08) + 0x6];
                    float tv = 1f - model.vertexBuffer[(x * 0x08) + 0x7];
                    OBJfs.WriteLine("v " + px.ToString("G") + " " + py.ToString("G") + " " + pz.ToString("G"));
                    OBJfs.WriteLine("vn " + nx.ToString("G") + " " + ny.ToString("G") + " " + nz.ToString("G"));
                    OBJfs.WriteLine("vt " + tu.ToString("G") + " " + tv.ToString("G"));
                }


                //Faces
                int textureNum = 0;
                for (int i = 0; i < model.indexBuffer.Length / 3; i++)
                {
                    int triIndex = i * 3;
                    if ((model.textureConfig != null) && (textureNum < model.textureConfig.Count) && (triIndex >= model.textureConfig[textureNum].start))
                    {
                        string modelId = model.textureConfig[textureNum].ID.ToString();
                        OBJfs.WriteLine("usemtl mtl_" + modelId);
                        OBJfs.WriteLine("g Texture_" + modelId);
                        textureNum++;
                    }

                    int f1 = model.indexBuffer[triIndex + 0] + 1;
                    int f2 = model.indexBuffer[triIndex + 1] + 1;
                    int f3 = model.indexBuffer[triIndex + 2] + 1;
                    OBJfs.WriteLine("f " + (f1 + "/" + f1 + "/" + f1) + " " + (f2 + "/" + f2 + "/" + f2) + " " + (f3 + "/" + f3 + "/" + f3));
                }
            }

        }

        private static void writeObjectMaterial(StreamWriter MTLfs, Model model, List<int> usedMtls)
        {
            for (int i = 0; i < model.textureConfig.Count; i++)
            {
                int modelTextureID = model.textureConfig[0].ID;
                if (!usedMtls.Contains(modelTextureID))
                {
                    MTLfs.WriteLine("newmtl mtl_" + modelTextureID);
                    MTLfs.WriteLine("Ns 1000");
                    MTLfs.WriteLine("Ka 1.000000 1.000000 1.000000");
                    MTLfs.WriteLine("Kd 1.000000 1.000000 1.000000");
                    MTLfs.WriteLine("Ni 1.000000");
                    MTLfs.WriteLine("d 1.000000");
                    MTLfs.WriteLine("illum 1");
                    MTLfs.WriteLine("map_Kd tex_" + model.textureConfig[i].ID + ".png");
                    usedMtls.Add(modelTextureID);
                }
            }
        }

        private static Vector3 rotate(Quaternion rotation, Vector3 v)
        {
            float s = rotation.W;
            Vector3 u = rotation.Xyz;

            float dotUV = Vector3.Dot(u, v);
            float dotUU = Vector3.Dot(u, u);

            Vector3 cross = Vector3.Cross(u, v);


            return new Vector3(
                2.0f * dotUV * u.X + ((s * s) - dotUU) * v.X + 2.0f * s * cross.X,
                2.0f * dotUV * u.Y + ((s * s) - dotUU) * v.Y + 2.0f * s * cross.Y,
                2.0f * dotUV * u.Z + ((s * s) - dotUU) * v.Z + 2.0f * s * cross.Z);
        }

        private static int writeObjectData(StreamWriter OBJfs, Model model, int faceOffset, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            int vertexCount = model.vertexBuffer.Length / 8;
            for (int x = 0; x < vertexCount; x++)
            {
                Vector3 v = new Vector3(
                    model.size * scale.X * model.vertexBuffer[(x * 0x08) + 0x0],
                    model.size * scale.Y * model.vertexBuffer[(x * 0x08) + 0x1],
                    model.size * scale.Z * model.vertexBuffer[(x * 0x08) + 0x2]);
                v = rotate(rotation, v);
                v += position;

                float nx = model.vertexBuffer[(x * 0x08) + 0x3];
                float ny = model.vertexBuffer[(x * 0x08) + 0x4];
                float nz = model.vertexBuffer[(x * 0x08) + 0x5];
                float tu = model.vertexBuffer[(x * 0x08) + 0x6];
                float tv = 1f - model.vertexBuffer[(x * 0x08) + 0x7];
                OBJfs.WriteLine("v " + v.X.ToString("G") + " " + v.Y.ToString("G") + " " + v.Z.ToString("G"));
                OBJfs.WriteLine("vn " + nx.ToString("G") + " " + ny.ToString("G") + " " + nz.ToString("G"));
                OBJfs.WriteLine("vt " + tu.ToString("G") + " " + tv.ToString("G"));
            }

            int textureNum = 0;
            for (int i = 0; i < model.indexBuffer.Length / 3; i++)
            {
                int triIndex = i * 3;
                if ((model.textureConfig != null) && (textureNum < model.textureConfig.Count) && (triIndex >= model.textureConfig[textureNum].start))
                {
                    string modelId = model.textureConfig[textureNum].ID.ToString();
                    OBJfs.WriteLine("usemtl mtl_" + modelId);
                    OBJfs.WriteLine("g Texture_" + modelId);
                    textureNum++;
                }

                int f1 = model.indexBuffer[triIndex + 0] + 1 + faceOffset;
                int f2 = model.indexBuffer[triIndex + 1] + 1 + faceOffset;
                int f3 = model.indexBuffer[triIndex + 2] + 1 + faceOffset;
                OBJfs.WriteLine("f " + (f1 + "/" + f1 + "/" + f1) + " " + (f2 + "/" + f2 + "/" + f2) + " " + (f3 + "/" + f3 + "/" + f3));
            }

            return vertexCount;
        }

        private static void WriteObjSeparate(string fileName, Level level, WriterLevelSettings settings)
        {
            string pathName = Path.GetDirectoryName(fileName);
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);

            List<TerrainFragment> terrain = new List<TerrainFragment>();

            for (int i = 0; i < level.terrainChunks.Count; i++)
            {
                if (settings.chunksSelected[i])
                {
                    terrain.AddRange(level.terrainChunks[i]);
                }
            }

            StreamWriter MTLfs = null; 

            if (settings.exportMTLFile) MTLfs = new StreamWriter(pathName + "\\" + fileNameNoExtension + ".mtl");

            using (StreamWriter OBJfs = new StreamWriter(fileName))
            {
                int faceOffset = 0;
                List<int> usedMtls = new List<int>();

                foreach (TerrainFragment t in terrain)
                {
                    OBJfs.WriteLine("o Object_" + t.model.id.ToString("X4"));
                    if (t.model.textureConfig != null)
                        OBJfs.WriteLine("mtllib " + fileNameNoExtension + ".mtl");
                    faceOffset += writeObjectData(OBJfs, t.model, faceOffset, Vector3.Zero, Vector3.One, Quaternion.Identity);
                    if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                }

                if (settings.writeTies)
                {
                    foreach (Tie t in level.ties)
                    {
                        OBJfs.WriteLine("o Object_" + t.model.id.ToString("X4"));
                        if (t.model.textureConfig != null)
                            OBJfs.WriteLine("mtllib " + fileNameNoExtension + ".mtl");
                        faceOffset += writeObjectData(OBJfs, t.model, faceOffset, t.position, t.scale, t.rotation);
                        if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                    }
                }

                if (settings.writeShrubs)
                {
                    foreach (Shrub t in level.shrubs)
                    {
                        OBJfs.WriteLine("o Object_" + t.model.id.ToString("X4"));
                        if (t.model.textureConfig != null)
                            OBJfs.WriteLine("mtllib " + fileNameNoExtension + ".mtl");
                        faceOffset += writeObjectData(OBJfs, t.model, faceOffset, t.position, t.scale, t.rotation);
                        if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                    }
                }

                if (settings.writeMobies)
                {
                    foreach (Moby t in level.mobs)
                    {
                        OBJfs.WriteLine("o Object_" + t.model.id.ToString("X4"));
                        if (t.model.textureConfig != null)
                            OBJfs.WriteLine("mtllib " + fileNameNoExtension + ".mtl");
                        faceOffset += writeObjectData(OBJfs, t.model, faceOffset, t.position, t.scale, t.rotation);
                        if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                    }
                }
            }

            if (settings.exportMTLFile) MTLfs.Dispose();
        }

        private static void WriteObjCombined(string fileName, Level level, WriterLevelSettings settings)
        {
            string pathName = Path.GetDirectoryName(fileName);
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);

            List<TerrainFragment> terrain = new List<TerrainFragment>();

            for (int i = 0; i < level.terrainChunks.Count; i++)
            {
                if (settings.chunksSelected[i])
                {
                    terrain.AddRange(level.terrainChunks[i]);
                }
            }

            StreamWriter MTLfs = null;

            if (settings.exportMTLFile) MTLfs = new StreamWriter(pathName + "\\" + fileNameNoExtension + ".mtl");

            using (StreamWriter OBJfs = new StreamWriter(fileName))
            {
                int faceOffset = 0;
                List<int> usedMtls = new List<int>();

                OBJfs.WriteLine("o Object_CombinedLevel");
                foreach (TerrainFragment t in terrain)
                {
                    faceOffset += writeObjectData(OBJfs, t.model, faceOffset, Vector3.Zero, Vector3.One, Quaternion.Identity);
                    if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                }

                if (settings.writeTies)
                {
                    foreach (Tie t in level.ties)
                    {
                        faceOffset += writeObjectData(OBJfs, t.model, faceOffset, t.position, t.scale, t.rotation);
                        if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                    }
                }

                if (settings.writeShrubs)
                {
                    foreach (Shrub t in level.shrubs)
                    {
                        faceOffset += writeObjectData(OBJfs, t.model, faceOffset, t.position, t.scale, t.rotation);
                        if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                    }
                }

                if (settings.writeMobies)
                {
                    foreach (Moby t in level.mobs)
                    {
                        faceOffset += writeObjectData(OBJfs, t.model, faceOffset, t.position, t.scale, t.rotation);
                        if (settings.exportMTLFile) writeObjectMaterial(MTLfs, t.model, usedMtls);
                    }
                }
            }

            if (settings.exportMTLFile) MTLfs.Dispose();
        }

        private static void WriteObjTypewise(string fileName, Level level, WriterLevelSettings settings)
        {
            throw new NotImplementedException();

            string pathName = Path.GetDirectoryName(fileName);
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);

            List<TerrainFragment> terrain = new List<TerrainFragment>();

            for (int i = 0; i < level.terrainChunks.Count; i++)
            {
                if (settings.chunksSelected[i])
                {
                    terrain.AddRange(level.terrainChunks[i]);
                }
            }
        }

        private static void WriteObjMaterialwise(string fileName, Level level, WriterLevelSettings settings)
        {
            throw new NotImplementedException();

            string pathName = Path.GetDirectoryName(fileName);
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);

            List<TerrainFragment> terrain = new List<TerrainFragment>();

            for (int i = 0; i < level.terrainChunks.Count; i++)
            {
                if (settings.chunksSelected[i])
                {
                    terrain.AddRange(level.terrainChunks[i]);
                }
            }
        }

        public static void WriteObj(string fileName, Level level, WriterLevelSettings settings)
        {
            switch(settings.mode)
            {
                case WriterLevelMode.Separate: 
                    WriteObjSeparate(fileName, level, settings);
                    return;
                case WriterLevelMode.Combined:
                    WriteObjCombined(fileName, level, settings);
                    return;
                case WriterLevelMode.Typewise:
                    WriteObjTypewise(fileName, level, settings);
                    return;
                case WriterLevelMode.Materialwise:
                    WriteObjMaterialwise(fileName, level, settings);
                    return;
            }
        }

        public enum WriterLevelMode{
            Separate,
            Combined,
            Typewise,
            Materialwise
        };

        public class WriterLevelSettings
        {
            public WriterLevelMode mode = WriterLevelMode.Combined;
            public bool writeTies = true;
            public bool writeShrubs = true;
            public bool writeMobies = true;
            public bool[] chunksSelected = new bool[5];
            public bool exportMTLFile = true;

            public WriterLevelSettings()
            {
                for (int i = 0; i < 5; i++)
                {
                    chunksSelected[i] = true;
                }
            }
        }
    }
}
