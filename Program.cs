using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program1
{
    public static short ToInt16(byte[] bytes, int startIndex, bool isLittleEndian)
    {
        if (isLittleEndian)
        {
            return (short)((bytes[startIndex + 1] << 8) | bytes[startIndex]);
        }
        else
        {
            return (short)((bytes[startIndex] << 8) | bytes[startIndex + 1]);
        }
    }
    public static byte ToByte(byte[] bytes, int offset)
    {
        return ((byte)BitConverter.ToChar(bytes, offset));
    }
    public static int ToInt32(byte[] bytes, int startIndex, bool isLittleEndian)
    {
        if (isLittleEndian)
        {
            return (bytes[startIndex + 3] << 24) | (bytes[startIndex + 2] << 16) | (bytes[startIndex + 1] << 8) | bytes[startIndex];
        }
        else
        {
            return (bytes[startIndex] << 24) | (bytes[startIndex + 1] << 16) | (bytes[startIndex + 2] << 8) | bytes[startIndex + 3];
        }
    }
    public static int ToInt24(byte[] bytes, int startIndex, bool isLittleEndian)
    {
        if (isLittleEndian)
        {
            return (bytes[startIndex + 2] << 16) | (bytes[startIndex + 1] << 8) | bytes[startIndex];
        }
        else
        {
            return (bytes[startIndex] << 16) | (bytes[startIndex + 1] << 8) | bytes[startIndex + 2];
        }
    }
    public static string InsertDecimal(int value, int decimalPlaces, bool isRight)
    {
        string str = value.ToString();

        if (value < 0)
        {
            decimalPlaces++;
        }

        if (isRight)
        {
            str = str.Insert(str.Length - decimalPlaces, ".");
        }
        else
        {
            str = str.Insert(decimalPlaces, ".");
        }

        return str;
    }
    public static string InsertDecimal(short value, int decimalPlaces, bool isRight)
    {
        string str = value.ToString();

        if (value < 0)
        {
            decimalPlaces++;
        }

        if (isRight)
        {
            str = str.Insert(str.Length - decimalPlaces, ".");
        }
        else
        {
            str = str.Insert(decimalPlaces, ".");
        }

        return str;
    }

    static void binparser(byte[] fileBytes)
    {
        byte header = ToByte(fileBytes, 0);
        //Console.WriteLine("Header: {0} or {1}", header, (char)header);
        byte temp = ToByte(fileBytes, 1);
        //Console.WriteLine("Throwaway Value: {0}", temp);
        short frmLen = ToInt16(fileBytes, 2, false);
        //Console.WriteLine("Frame Length: {0}", frmLen);
        //Console.WriteLine("Actual Data Length: {0}", fileBytes.Length);
        byte mpFlag = ToByte(fileBytes, 4);
        //Console.WriteLine("Multi-Packet Flag: {0}",mpFlag);

        byte resFldLen;
        int hdrLen;
        if (mpFlag != 0) //mpFlag = 80H
        {
            byte[] IMEIbytes = new byte[8];
            Array.Copy(fileBytes, 7, IMEIbytes, 0, 8);
            string IMEI = BitConverter.ToString(IMEIbytes);
            IMEI = IMEI.Replace("-", "");
            IMEI = IMEI.Substring(IMEI.Length - 6);
            Console.WriteLine(IMEI);

            resFldLen = ToByte(fileBytes, 20);
            hdrLen = 21;
        }
        else //mpFlag = 00H
        {
            byte[] IMEIbytes = new byte[8];
            Array.Copy(fileBytes, 5, IMEIbytes, 0, 8);
            string IMEI = BitConverter.ToString(IMEIbytes);
            IMEI = IMEI.Replace("-", "");
            IMEI = IMEI.Substring(IMEI.Length - 6);
            Console.WriteLine(IMEI);

            resFldLen = ToByte(fileBytes, 18);
            hdrLen = 19;
        }

        //byte resFldLen = (byte)BitConverter.ToChar(fileBytes, byteCount);
        //Console.WriteLine("Reserved Field Length: {0}", resFldLen);
        hdrLen = hdrLen + resFldLen;

        frmLen = (short)(frmLen - hdrLen);
        //Console.WriteLine("Remaining Length of Records: {0}", frmLen);

        //ENTERING RECORD SECTION
        int currentByte = hdrLen;

        while (frmLen > 4)
        {
            byte rdLen1 = ToByte(fileBytes, currentByte);
            //Console.WriteLine("1st Byte Record Length: {0}", rdLen1);
            short rdLength;
            short dataLength;
            byte rid;

            if (rdLen1 > 127) //2 bytes for record length
            {
                byte modifiedrdLen1 = (byte)(rdLen1 & 0b01111111);
                byte rdLen2 = (byte)BitConverter.ToChar(fileBytes, currentByte + 1);
                //Console.WriteLine("2nd Byte Record Length: {0}", rdLen2);
                rdLength = (short)(modifiedrdLen1 << 8 | rdLen2);
                dataLength = (short)(rdLength - 8);
                frmLen = (short)(frmLen - 8);
                currentByte = currentByte + 6;
                rid = ToByte(fileBytes, currentByte);
                currentByte = currentByte + 2;
            }
            else
            {
                rdLength = rdLen1;
                dataLength = (short)(rdLength - 7);
                frmLen = (short)(frmLen - 7);
                currentByte = currentByte + 5;
                rid = ToByte(fileBytes, currentByte);
                currentByte = currentByte + 2;
            }
            Console.WriteLine("Report ID: {0}", rid);
            if (rid != 80) //Only parse fixed interval reporting
            {
                return;
            }
            
            // at this point, need to use record length to make sure we get to end of record and also read data from here
            while (dataLength > 0)
            {
                byte dataID1 = ToByte(fileBytes, currentByte);
                Console.WriteLine("Data ID Byte 1: {0}", dataID1);
                if (dataID1 == 82) //FULL LOCATION DATA ID
                {
                    currentByte = currentByte + 1; //skip dataID
                    byte dataLen1 = ToByte(fileBytes, currentByte);
                    currentByte = currentByte + 1;
                    dataLength = (short)(dataLength - 2);
                    frmLen = (short)(frmLen - 2);

                    //PARSE DATA CONTENT

                    byte fixMode = ToByte(fileBytes, currentByte);
                    currentByte = currentByte + 1;
                    if (fixMode == 8 | fixMode == 9 | fixMode == 5)
                    {
                        int longitude = ToInt32(fileBytes, currentByte, false);
                        string longitudestr = InsertDecimal(longitude, 3, false);
                        currentByte = currentByte + 4;
                        int latitude = ToInt32(fileBytes, currentByte, false);
                        string latitudestr = InsertDecimal(latitude, 2, false);
                        currentByte = currentByte + 4;
                        int utcTime = ToInt32(fileBytes, currentByte, false);
                        DateTime dateTime = new DateTime(1970, 1, 1).AddSeconds(utcTime).ToUniversalTime();
                        string utc = dateTime.ToString("MM/dd/yyyy h:mm:ss tt");
                        currentByte = currentByte + 4;
                        short speed = ToInt16(fileBytes, currentByte, false);
                        string speedstr = "0";
                        if (speed > 0)
                        {
                            speedstr = InsertDecimal(speed, 1, true);
                        }
                        currentByte = currentByte + 3;
                        short azimuth = ToInt16(fileBytes, currentByte, false);
                        azimuth = Convert.ToInt16(azimuth);
                        string azimuthstr = Convert.ToString(azimuth);
                        currentByte = currentByte + 2;
                        int altitude = ToInt24(fileBytes, currentByte, false);
                        string altitudestr = "0";
                        if (altitude > 0)
                        {
                            altitudestr = InsertDecimal(altitude, 1, true);
                            int index = altitudestr.IndexOf('.');
                            altitudestr = altitudestr.Substring(0, index);
                        }
                        currentByte = currentByte + 4;
                        Console.WriteLine("Lat: {0} Lon: {1} Crs:{2} Alt:{3} Spd:{4} Utc: {5}", latitudestr, longitudestr, azimuthstr, altitudestr, speedstr, utc);
                    }
                    else
                    {
                        Console.WriteLine("Fix state {0}", fixMode);
                        currentByte = currentByte + 21;
                    }
                    dataLength = (short)(dataLength - dataLen1);
                    frmLen = (short)(frmLen - dataLen1);
                }
                else if (dataID1 == 87) //EXTERNAL POWER VOLTAGE
                {
                    currentByte = currentByte + 1; //skip dataID
                    byte dataLen1 = ToByte(fileBytes, currentByte);
                    currentByte = currentByte + 1;
                    dataLength = (short)(dataLength - 2);
                    frmLen = (short)(frmLen - 2);

                    //PARSE DATA CONTENT

                    byte connStatus = ToByte(fileBytes, currentByte);
                    currentByte = currentByte + 1;
                    if (connStatus == 1)
                    {
                        short milliVolts = ToInt16(fileBytes, currentByte, false);
                        Console.WriteLine("Battery Voltage {0} {1}", milliVolts/1000, (milliVolts / 12000)*100);
                        currentByte += 2;
                    }
                    dataLength = (short)(dataLength - dataLen1);
                    frmLen = (short)(frmLen - dataLen1);
                }
                else if (dataID1 == 140) //HARSH BEHAVIOR
                {
                    currentByte = currentByte + 2; //skip dataID
                    byte dataLen1 = ToByte(fileBytes, currentByte);
                    currentByte = currentByte + 1;
                    dataLength = (short)(dataLength - 2);
                    frmLen = (short)(frmLen - 2);

                    //PARSE DATA CONTENT
                    byte type = ToByte(fileBytes, currentByte);
                    currentByte = currentByte + 1;
                    short maxValue = ToInt16(fileBytes, currentByte, false);
                    switch (type)
                    {
                        case 1: 
                            Console.WriteLine("Harsh Acceleration {0} mg",maxValue );
                            break;
                        case 2:
                            Console.WriteLine("Harsh Deceleration {0} mg", maxValue);
                            break;
                        case 3:
                            Console.WriteLine("Harsh Cornering {0} mg", maxValue);
                            break;
                        case 0:
                            Console.WriteLine("Normal Acceleration {0} mg", maxValue);
                            break;
                        default:
                            Console.WriteLine("Unknwown Acceleration {0} mg", maxValue);
                            break;
                    }
                    currentByte += 8;
                    dataLength = (short)(dataLength - dataLen1);
                    frmLen = (short)(frmLen - dataLen1);
                }
                else //NOT FULL LOCATION ID
                {
                    if (dataID1 > 127) //2 byte data ID
                    {
                        currentByte = currentByte + 2; //skip dataID
                        byte dataLen1 = ToByte(fileBytes, currentByte);
                        short dataCtLen;
                        if (dataLen1 > 127)
                        {
                            byte modifieddataLen1 = (byte)(dataLen1 & 0b01111111);
                            byte dataLen2 = (byte)BitConverter.ToChar(fileBytes, currentByte + 1);
                            dataCtLen = (short)(dataLen1 << 8 | dataLen2);
                            currentByte = currentByte + 2;
                            dataLength = (short)(dataLength - 4);
                            frmLen = (short)(frmLen - 4);
                        }
                        else
                        {
                            dataCtLen = dataLen1;
                            currentByte = currentByte + 1;
                            dataLength = (short)(dataLength - 3);
                            frmLen = (short)(frmLen - 3);
                        }
                        currentByte = currentByte + dataCtLen;
                        dataLength = (short)(dataLength - dataCtLen);
                        frmLen = (short)(frmLen - dataCtLen);
                    }
                    else //1 byte data ID
                    {
                        currentByte = currentByte + 1; //skip dataID
                        byte dataLen1 = ToByte(fileBytes, currentByte);
                        short dataCtLen;
                        if (dataLen1 > 127)
                        {
                            byte modifieddataLen1 = (byte)(dataLen1 & 0b01111111);
                            byte dataLen2 = (byte)BitConverter.ToChar(fileBytes, currentByte + 1);
                            dataCtLen = (short)(dataLen1 << 8 | dataLen2);
                            currentByte = currentByte + 2;
                            dataLength = (short)(dataLength - 3);
                            frmLen = (short)(frmLen - 3);
                        }
                        else
                        {
                            dataCtLen = dataLen1;
                            currentByte = currentByte + 1;
                            dataLength = (short)(dataLength - 2);
                            frmLen = (short)(frmLen - 2);
                        }
                        currentByte = currentByte + dataCtLen;
                        dataLength = (short)(dataLength - dataCtLen);
                        frmLen = (short)(frmLen - dataCtLen);
                    }
                }
            }
        }
    }

    public static void Main(string[] args)
    {
        int count = 0;
        using (FileStream fs = new FileStream("C:\\Users\\ashwinangle30\\ConsoleApp1\\ConsoleApp1\\log.bin", FileMode.Open, FileAccess.Read))
        {
            using (BinaryReader r = new BinaryReader(fs))
            {
                while (r.BaseStream.Position != r.BaseStream.Length)
                {
                    var data = new List<byte>();
                    for (int i = 0; ; i++)
                    {
                        byte read = r.ReadByte();
                        if (read == '\n')
                        {
                            if (data.Count <= 0)
                            {
                                continue;
                            }
                            break;
                        }
                        data.Add(read);
                    }
                    byte[] recBytes = data.ToArray();
                    if ((recBytes[0] == '-' || recBytes[0] == '+') && recBytes[recBytes.Length - 1] == '$' && 
                        (recBytes[1] != 'A' && recBytes[1] != 'C' && recBytes[1] != 'K' && recBytes[1] != ':'))
                    {
                        try
                        {
                            Console.Write("{0}: ", count++);
                            binparser(recBytes);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Bytes read: " + recBytes.Length);
                            for (int i = 0; i < recBytes.Length; i++)
                            {
                                Console.WriteLine("{0}\t{1}", recBytes[i], Convert.ToChar(recBytes[i]));
                            }
                            Console.Write("\n");
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }
    Console.WriteLine("Parse log file end");
    }
}
