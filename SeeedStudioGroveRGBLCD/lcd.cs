using System;
using System.Threading;
using System.IO.Ports;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using Toolbox.NETMF;
using Toolbox.NETMF.Hardware;

namespace SeeedStudio.Grove.RGBLCD
{
    class LCD
    {
        private MultiI2C _lcd;
        private MultiI2C _lcdbacklight;

        private byte _displayfunction = 0x00;
        private byte _displaycontrol = 0x00;
        private byte _displaymode = 0x00;
        private byte _initialized = 0x00;
        private byte _numlines = 0x00;
        private byte _currline = 0x00;

        // Device I2C Arress
        //private byte LCD_ADDRESS = (0x7c>>1);
        //private byte RGB_ADDRESS = (0xc4>>1);
        private byte RGB_ADDRESS = (0x62);
        private byte LCD_ADDRESS = (0x3E);

        private byte REG_RED = 0x04;    // pwm2
        private byte REG_GREEN = 0x03;  // pwm1
        private byte REG_BLUE = 0x02;   // pwm0

        // commands
        private byte LCD_CLEARDISPLAY = 0x01;
        private byte LCD_RETURNHOME = 0x02;
        private byte LCD_ENTRYMODESET = 0x04;
        private byte LCD_DISPLAYCONTROL = 0x08;
        private byte LCD_CURSORSHIFT = 0x10;
        private byte LCD_FUNCTIONSET = 0x20;
        private byte LCD_SETCGRAMADDR = 0x40;
        private byte LCD_SETDDRAMADDR = 0x80;

        // flags for display entry mode
        private byte LCD_ENTRYRIGHT = 0x00;
        private byte LCD_ENTRYLEFT = 0x02;
        private byte LCD_ENTRYSHIFTINCREMENT = 0x01;
        private byte LCD_ENTRYSHIFTDECREMENT = 0x00;

        // flags for display on/off control
        private byte LCD_DISPLAYON = 0x04;
        private byte LCD_DISPLAYOFF = 0x00;
        private byte LCD_CURSORON = 0x02;
        private byte LCD_CURSOROFF = 0x00;
        private byte LCD_BLINKON = 0x01;
        private byte LCD_BLINKOFF = 0x00;

        // flags for display/cursor shift
        private byte LCD_DISPLAYMOVE = 0x08;
        private byte LCD_CURSORMOVE = 0x00;
        private byte LCD_MOVERIGHT = 0x04;
        private byte LCD_MOVELEFT = 0x00;

        // flags for function set
        private byte LCD_8BITMODE = 0x10;
        private byte LCD_4BITMODE = 0x00;
        private byte LCD_2LINE = 0x08;
        private byte LCD_1LINE = 0x00;
        private byte LCD_5x10DOTS = 0x04;
        private byte LCD_5x8DOTS = 0x00;

        byte[][] color_define = 
        {
            new byte[] {255, 255, 255},     // white
            new byte[] {255, 0, 0},         // red
            new byte[] {0, 255, 0},         // green
            new byte[] {0, 0, 255}          // blue
        };

        public LCD() 
        {

            _lcd = new MultiI2C(0x3E, 100);

            _lcdbacklight = new MultiI2C(0x62, 100);

            _displayfunction |= LCD_2LINE;
            _numlines = 2;
            _currline = 0;

            // SEE PAGE 45/46 FOR INITIALIZATION SPECIFICATION!
            // according to datasheet, we need at least 40ms after power rises above 2.7V
            // before sending commands. Arduino can turn on way befer 4.5V so we'll wait 50
            Thread.Sleep(50);


            // this is according to the hitachi HD44780 datasheet
            // page 45 figure 23

            // Send function set command sequence
            command((byte)(LCD_FUNCTIONSET | _displayfunction));
            Thread.Sleep(5);  // wait more than 4.1ms

            // second try
            command((byte)(LCD_FUNCTIONSET | _displayfunction));
            Thread.Sleep(1);

            // third go
            command((byte)(LCD_FUNCTIONSET | _displayfunction));


            // finally, set # lines, font size, etc.
            command((byte)(LCD_FUNCTIONSET | _displayfunction));

            // turn the display on with no cursor or blinking default
            _displaycontrol = (byte)(LCD_DISPLAYON | LCD_CURSORON | LCD_BLINKON);
            display();

            // clear it off
            clear();

            // Initialize to default text direction (for romance languages)
            _displaymode = (byte)(LCD_ENTRYLEFT | LCD_ENTRYSHIFTDECREMENT);
            // set the entry mode
            command((byte)(LCD_ENTRYMODESET | _displaymode));
    
    
            // backlight init
            setReg(0, 0);
            setReg(1, 0);
            setReg(0x08, 0xAA);     // all led control by pwm
    
            setRGB(255, 255, 255);
            //setColor("white");
        }

        private void i2c_send_byte(byte[] dta)
        {
            _lcd.Write(dta);
        }

        /********** high level commands, for the user! */
        public void clear()
        {
            command(LCD_CLEARDISPLAY);        // clear display, set cursor position to zero
            Thread.Sleep(2);          // this command takes a long time!
        }

        public void home()
        {
            command(LCD_RETURNHOME);        // set cursor position to zero
            Thread.Sleep(2);        // this command takes a long time!
        }

        public void SetCursor(int col, int row)
        {

            col = (row == 0 ? col|0x80 : col|0xc0);
            byte[] dta = {0x80, (byte)(col)};

            i2c_send_byte(dta);

        }

        // Turn the display on/off (quickly)
        public void noDisplay()
        {
            _displaycontrol &= (byte)(~LCD_DISPLAYON);
            command((byte)(LCD_DISPLAYCONTROL | _displaycontrol));
        }

        public void display() {
            _displaycontrol |= LCD_DISPLAYON;
            command((byte)(LCD_DISPLAYCONTROL | _displaycontrol));
        }

        // Turns the underline cursor on/off
        public void noCursor()
        {
            _displaycontrol &= (byte)(~LCD_CURSORON);
            command((byte)(LCD_DISPLAYCONTROL | _displaycontrol));
        }

        public void cursor() {
            _displaycontrol |= LCD_CURSORON;
            command((byte)(LCD_DISPLAYCONTROL | _displaycontrol));
        }

        // Turn on and off the blinking cursor
        public void noBlink()
        {
            _displaycontrol &= (byte)(~LCD_BLINKON);
            command((byte)(LCD_DISPLAYCONTROL | _displaycontrol));
        }
        public void blink()
        {
            _displaycontrol |= LCD_BLINKON;
            command((byte)(LCD_DISPLAYCONTROL | _displaycontrol));
        }

        // These commands scroll the display without changing the RAM
        public void scrollDisplayLeft()
        {
            command((byte)(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVELEFT));
        }
        public void scrollDisplayRight()
        {
            command((byte)(LCD_CURSORSHIFT | LCD_DISPLAYMOVE | LCD_MOVERIGHT));
        }

        // This is for text that flows Left to Right
        public void leftToRight()
        {
            _displaymode |= LCD_ENTRYLEFT;
            command((byte)(LCD_ENTRYMODESET | _displaymode));
        }

        // This is for text that flows Right to Left
        public void rightToLeft()
        {
            _displaymode &= (byte)(~LCD_ENTRYLEFT);
            command((byte)(LCD_ENTRYMODESET | _displaymode));
        }

        // This will 'right justify' text from the cursor
        public void autoscroll()
        {
            _displaymode |= LCD_ENTRYSHIFTINCREMENT;
            command((byte)(LCD_ENTRYMODESET | _displaymode));
        }

        // This will 'left justify' text from the cursor
        public void noAutoscroll()
        {
            _displaymode &= (byte)(~LCD_ENTRYSHIFTINCREMENT);
            command((byte)(LCD_ENTRYMODESET | _displaymode));
        }

        // Allows us to fill the first 8 CGRAM locations
        // with custom characters
        public void createChar(byte location, byte[] charmap)
        {

            location &= 0x7; // we only have 8 locations 0-7
            command((byte)(LCD_SETCGRAMADDR | (location << 3)));
    
    
            byte[] dta = {0x00};
            dta[0] = 0x40;
            for(int i=0; i<8; i++)
            {
                dta[i+1] = charmap[i];
            }
            i2c_send_byte(dta);
        }

        /*********** mid level commands, for sending data/cmds */

        // send command
        private void command(byte value)
        {
            byte[] dta = {0x80, value};
            i2c_send_byte(dta);
        }

        // send data
        public int write(byte[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                byte[] dta = { 0x40, value[i] };
                i2c_send_byte(dta);
            }
            return 1; // assume sucess
        }

        private void setReg(byte addr, byte dta)
        {
            byte[] baddr = { addr, dta };
            _lcdbacklight.Write(baddr);
        }

        public void setRGB(byte r = 0, byte g = 255, byte b = 0) //default to green
        {
            setReg(REG_RED, r);
            setReg(REG_GREEN, g);
            setReg(REG_BLUE, b);
        }

        public void setColor(string color)
        {
            int colorint;
            if (color.ToUpper() == "WHITE")
                colorint = 0;
            else if (color.ToUpper() == "RED")
                colorint = 1;
            else if (color.ToUpper() == "GREEN")
                colorint = 2;
            else if (color.ToUpper() == "BLUE")
                colorint = 3;
            else
                return;

            setRGB(color_define[colorint][0], color_define[colorint][1], color_define[colorint][2]);
        }
    }
}
