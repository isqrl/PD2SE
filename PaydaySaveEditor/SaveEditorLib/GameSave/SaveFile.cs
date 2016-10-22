using System;
using System.Collections.Generic;
using System.IO;

/*
GameSave struct is as follows:

struct GameSave
{
	int	VERSION
	Block header
	Block gamedata
	Block footer
	byte[16] Padding
	byte[16] Special MD5
}

where Block has the following structure:

struct Block
{
	int	SIZE
	int	VERSION
	byte[size - 16] data
	byte[16] MD5(data)
}

The gamedata block appears to contain a serialized dictionary
*/

namespace PD2.GameSave
{
    public class SaveFile
    {
        private const int SAVE_VERSION = 10; // BETA = 9, RETAIL = 10

        private String filePath;

        private DataBlock header;
        private GameDataBlock gamedata;
        private DataBlock footer;

        public Dictionary<Object, Object> GameData
        {
            get { return gamedata.Dictionary; }
            set { gamedata.Dictionary = value; }
        }

        public SaveFile(String filePath)
        {
            this.filePath = filePath;
        }

        public static SaveFile Load(string filePath, bool encrypted = true)
        {
            byte[] file = File.ReadAllBytes(filePath);
            byte[] data = encrypted ? Encryption.TransformData(file) : file;

            // Open save to process
            using (BinaryReader br = new BinaryReader(new MemoryStream(data)))
            {
                // Validate that file is a supported save format
                int version = br.ReadInt32();
                if (version != SAVE_VERSION)
                {
                    throw new Exception(String.Format("Unsupported save version: {0}", version.ToString("X4")));
                }

                return new SaveFile(filePath)
                {
                    header = new DataBlock(br),
                    gamedata = new GameDataBlock(br),
                    footer = new DataBlock(br)
                };
            }
        }

        public static void Save(SaveFile saveFile, bool encrypt = true)
        {
            Save(saveFile, saveFile.filePath, encrypt);
        }

        public static void Save(SaveFile saveFile, string outputPath, bool encrypt = true)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    // Rebuild save
                    bw.Write(SAVE_VERSION);
                    bw.Write(saveFile.header.ToArray());
                    bw.Write(saveFile.gamedata.ToArray());
                    bw.Write(saveFile.footer.ToArray());
                    bw.Write(new byte[16]);
                    bw.Write(Encryption.GenerateSaveHash(ms.ToArray()));

                    // Store save and close streams
                    byte[] data = ms.ToArray();

                    // Encrypt if requested
                    if (encrypt) data = Encryption.TransformData(data);
                    File.WriteAllBytes(outputPath, data);
                }
            }
        }
    }
}