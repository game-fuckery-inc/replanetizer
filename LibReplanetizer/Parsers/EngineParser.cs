﻿using LibReplanetizer.Headers;
using LibReplanetizer.LevelObjects;
using LibReplanetizer.Models;
using LibReplanetizer.Models.Animations;
using System;
using System.Collections.Generic;
using static LibReplanetizer.DataFunctions;

namespace LibReplanetizer.Parsers
{
    public class EngineParser : RatchetFileParser, IDisposable
    {
        EngineHeader engineHead;

        public EngineParser(string engineFile) : base(engineFile)
        {
            engineHead = new EngineHeader(fileStream);
        }

        public GameType GetGameType()
        {
            return engineHead.game;
        }

        public List<Model> GetMobyModels()
        {
            return GetMobyModels(engineHead.game, engineHead.mobyModelPointer);
        }

        public List<Model> GetTieModels()
        {
            return GetTieModels(engineHead.tieModelPointer, engineHead.tieModelCount);
        }

        public List<Model> GetShrubModels()
        {
            return GetShrubModels(engineHead.shrubModelPointer, engineHead.shrubModelCount);
        }

        public List<Texture> GetTextures()
        {
            return GetTextures(engineHead.texturePointer, engineHead.textureCount);
        }

        public List<Tie> GetTies(List<Model> tieModels)
        {
            return GetTies(tieModels, engineHead.tiePointer, engineHead.tieCount);
        }

        public List<Light> GetLights()
        {
            return GetLights(engineHead.lightPointer, engineHead.lightCount);
        }

        public List<Shrub> GetShrubs(List<Model> shrubModels)
        {
            return GetShrubs(shrubModels, engineHead.shrubPointer, engineHead.shrubCount);
        }

        public List<TerrainFragment> GetTerrainModels()
        {
            return GetTerrainModels(engineHead.terrainPointer);
        }

        public SkyboxModel GetSkyboxModel()
        {
            return GetSkyboxModel(engineHead.game, engineHead.skyboxPointer);
        }

        public List<UiElement> GetUiElements()
        {
            return GetUiElements(engineHead.uiElementPointer);
        }

        public List<Animation> GetPlayerAnimations(MobyModel ratchet)
        {
            if (engineHead.game.num == 4) return new List<Animation>();

            return GetPlayerAnimations(engineHead.playerAnimationPointer, ratchet);
        }

        public List<Model> GetGadgets()
        {
            return GetGadgets(engineHead.game, engineHead.gadgetPointer, engineHead.gadgetCount);
        }

        public LightConfig GetLightConfig()
        {
            return GetLightConfig(engineHead.lightConfigPointer);
        }

        public List<int> GetTextureConfigMenu()
        {
            return GetTextureConfigMenu(engineHead.textureConfigMenuPointer, engineHead.textureConfigMenuCount);
        }

        public Model GetCollisionModel()
        {
            return GetCollisionModel(engineHead.collisionPointer);
        }

        public byte[] GetRenderDefBytes()
        {
            if (engineHead.renderDefPointer > 0)
            {
                return ReadArbBytes(engineHead.renderDefPointer, engineHead.collisionPointer - engineHead.renderDefPointer);
            }
            else
            {
                return new byte[0];
            }
        }

        public byte[] GetCollisionBytes()
        {
            if (engineHead.collisionPointer > 0)
            {
                byte[] headBlock = ReadBlock(fileStream, engineHead.collisionPointer, 8);
                int collisionStart = engineHead.collisionPointer + ReadInt(headBlock, 0);
                int collisionLength = ReadInt(headBlock, 4);
                int totalLength = collisionStart + collisionLength - engineHead.collisionPointer;

                return ReadArbBytes(engineHead.collisionPointer, totalLength);
            }
            else
            {
                return new byte[0];
            }
        }

        public byte[] GetBillboardBytes()
        {
            return ReadArbBytes(engineHead.texture2dPointer, engineHead.soundConfigPointer - engineHead.texture2dPointer);
        }

        public byte[] GetSoundConfigBytes()
        {
            return ReadArbBytes(engineHead.soundConfigPointer, engineHead.lightPointer - engineHead.soundConfigPointer);
        }

        public void Dispose()
        {
            fileStream.Close();
        }
    }
}
