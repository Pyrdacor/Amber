﻿using Ambermoon.Data.Legacy.Serialization;
using Amber.GameData.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Amber.GameData.Pyrdacor.FileSpecs
{
    internal class MerchantData : IFileSpec
    {
        public string Magic => "MER";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<RLE0>();
        Merchant? merchant = null;

        public MerchantData()
        {

        }

        public MerchantData(Merchant merchant)
        {
            this.merchant = merchant;
        }

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            merchant = Merchant.Load(new MerchantReader(), dataReader);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (merchant == null)
                throw new AmberException(ExceptionScope.Application, "Merchant data was null when trying to write it.");

            new MerchantWriter().WriteMerchant(merchant, dataWriter);
        }
    }
}
