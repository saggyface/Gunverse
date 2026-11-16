param(
    [string]$OutputDirectory = "C:\Unity\UnityProjects\Gun Verse\Assets\_GunVerse\Textures\ItemLabels"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$items = @(
    @{ File = "SoupCanGun.png"; Text = "SOUP"; Background = "#B52B22"; Foreground = "#FFF2D8"; Accent = "#F0C46A" },
    @{ File = "CerealBoxGun.png"; Text = "CEREAL"; Background = "#E0B42A"; Foreground = "#4A2A12"; Accent = "#FFF1B8" },
    @{ File = "MilkCartonGun.png"; Text = "MILK"; Background = "#EAF2F5"; Foreground = "#244A6A"; Accent = "#5C91C2" },
    @{ File = "BreadLoafGun.png"; Text = "BREAD"; Background = "#B9783A"; Foreground = "#FFF0D0"; Accent = "#5A2F16" },
    @{ File = "DetergentBottleGun.png"; Text = "SOAP"; Background = "#2FA96D"; Foreground = "#FFFFFF"; Accent = "#B8F0D4" },
    @{ File = "TomatoCanGun.png"; Text = "TOMATO"; Background = "#C62828"; Foreground = "#FFF0D0"; Accent = "#FFC857" },
    @{ File = "CheckoutCounterGun.png"; Text = "CHECKOUT"; Background = "#1F4D3D"; Foreground = "#F7E8C4"; Accent = "#E3B85A" },
    @{ File = "CashRegisterGun.png"; Text = "REGISTER"; Background = "#343A40"; Foreground = "#F2F2F2"; Accent = "#8FA8A0" },
    @{ File = "ConveyorBeltGun.png"; Text = "BELT"; Background = "#4A4F55"; Foreground = "#F0F0F0"; Accent = "#9AA0A6" },
    @{ File = "LaneMarkerGun.png"; Text = "LANE"; Background = "#A62E2E"; Foreground = "#FFF4D6"; Accent = "#E8C15C" },
    @{ File = "CeilingLightGun.png"; Text = "LIGHT"; Background = "#F5E7B8"; Foreground = "#4A3B1E"; Accent = "#FFFFFF" },
    @{ File = "EntranceMatGun.png"; Text = "WELCOME"; Background = "#2C5A45"; Foreground = "#FFF2D8"; Accent = "#C9A85A" },
    @{ File = "TVGun.png"; Text = "TV"; Background = "#0B2D5C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "BatteryGun.png"; Text = "BATTERY"; Background = "#1E6B3A"; Foreground = "#FFF7D1"; Accent = "#D7FF5F" },
    @{ File = "SpeakerGun.png"; Text = "SPEAKER"; Background = "#24262C"; Foreground = "#F0F3F8"; Accent = "#4BB9FF" },
    @{ File = "KeyboardGun.png"; Text = "KEYBOARD"; Background = "#3A3E48"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "LaptopGun.png"; Text = "LAPTOP"; Background = "#5B6472"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "PhoneGun.png"; Text = "PHONE"; Background = "#1D4A83"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "PrinterGun.png"; Text = "PRINTER"; Background = "#E8EDF3"; Foreground = "#19355C"; Accent = "#FFD34E" },
    @{ File = "MicrowaveGun.png"; Text = "MICROWAVE"; Background = "#59616C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "GuysquadCounterGun.png"; Text = "GUYSQUAD"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "BopBuySignGun.png"; Text = "BOPBUY"; Background = "#0D4F9E"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "HeadphonesGun.png"; Text = "HEADPHONES"; Background = "#243046"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "MouseGun.png"; Text = "MOUSE"; Background = "#343A44"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "MonitorGun.png"; Text = "MONITOR"; Background = "#1D3C66"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "ComputerGun.png"; Text = "COMPUTER"; Background = "#20242C"; Foreground = "#FFFFFF"; Accent = "#4BB9FF" },
    @{ File = "RadioGun.png"; Text = "RADIO"; Background = "#5B3720"; Foreground = "#FFF2D8"; Accent = "#E3B85A" },
    @{ File = "ToasterGun.png"; Text = "TOASTER"; Background = "#B8C2CC"; Foreground = "#24303C"; Accent = "#FFD34E" },
    @{ File = "ElectricOvenGun.png"; Text = "OVEN"; Background = "#3B414A"; Foreground = "#FFFFFF"; Accent = "#FF8A3D" },
    @{ File = "WasherGun.png"; Text = "WASHER"; Background = "#E3E9EF"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "AirConditionerGun.png"; Text = "AIR CON"; Background = "#D7E2EA"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "LargeSpeakerGun.png"; Text = "SPEAKER"; Background = "#1A1C22"; Foreground = "#FFFFFF"; Accent = "#4BB9FF" },
    @{ File = "RouterGun.png"; Text = "ROUTER"; Background = "#213A32"; Foreground = "#FFFFFF"; Accent = "#7CF0B4" },
    @{ File = "FlipPhoneGun.png"; Text = "FLIP PHONE"; Background = "#3B2D5C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "TV32Gun.png"; Text = "TV 32"; Background = "#0B2D5C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "TV65Gun.png"; Text = "TV 65"; Background = "#0B2D5C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "TV75Gun.png"; Text = "TV 75"; Background = "#0B2D5C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "MobileSignGun.png"; Text = "MOBILE"; Background = "#0D4F9E"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "ComputersSignGun.png"; Text = "COMPUTERS"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "HomeTheatreSignGun.png"; Text = "HOME THEATRE"; Background = "#101820"; Foreground = "#FFFFFF"; Accent = "#4BB9FF" },
    @{ File = "AppliancesSignGun.png"; Text = "APPLIANCES"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "CheckoutSignGun.png"; Text = "CHECKOUT"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "TvWallSignGun.png"; Text = "TV WALL"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "SmartHomeSignGun.png"; Text = "SMART HOME"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "CellPhonesSignGun.png"; Text = "CELL PHONES"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "MobileAccessoriesSignGun.png"; Text = "ACCESSORIES"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "PrintersInkSignGun.png"; Text = "PRINTERS"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "TabletsSignGun.png"; Text = "TABLETS"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "LaptopsSignGun.png"; Text = "LAPTOPS"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "ComputingSignGun.png"; Text = "COMPUTING"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "OrderPickupSignGun.png"; Text = "ORDER PICKUP"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "CustomerServiceSignGun.png"; Text = "CUSTOMER SVC"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "HolidaySignGun.png"; Text = "HOLIDAY"; Background = "#D9A400"; Foreground = "#242424"; Accent = "#FFFFFF" },
    @{ File = "GiftsSignGun.png"; Text = "GIFTS"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "StarlinkDishGun.png"; Text = "SATELLITE"; Background = "#203040"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "GiftBoxGun.png"; Text = "GIFT"; Background = "#B52B22"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "TVBoxGun.png"; Text = "TV BOX"; Background = "#C78944"; Foreground = "#2A1A0E"; Accent = "#FFD34E" },
    @{ File = "InkBoxGun.png"; Text = "INK"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "TabletGun.png"; Text = "TABLET"; Background = "#1D3C66"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "DisplayTableGun.png"; Text = "TABLE"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "PlinthGun.png"; Text = "PLINTH"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "DisplayLedgeGun.png"; Text = "LEDGE"; Background = "#2A3038"; Foreground = "#FFFFFF"; Accent = "#AEB8C4" },
    @{ File = "GreeterTableGun.png"; Text = "GREETER"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "PodiumGun.png"; Text = "PODIUM"; Background = "#E3E9EF"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "StoolGun.png"; Text = "STOOL"; Background = "#5B3A22"; Foreground = "#FFF2D8"; Accent = "#C9A85A" },
    @{ File = "StanchionGun.png"; Text = "QUEUE"; Background = "#20242C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "ShoppingCartGun.png"; Text = "CART"; Background = "#174A8A"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "CartCorralGun.png"; Text = "CART CORRAL"; Background = "#4A5058"; Foreground = "#FFFFFF"; Accent = "#AEB8C4" },
    @{ File = "EasPedestalGun.png"; Text = "SECURITY"; Background = "#E3E9EF"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "BeverageCoolerGun.png"; Text = "COOLER"; Background = "#7A1717"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "LockerGun.png"; Text = "LOCKER"; Background = "#59616C"; Foreground = "#FFFFFF"; Accent = "#AEB8C4" },
    @{ File = "SecurityCameraGun.png"; Text = "CAMERA"; Background = "#20242C"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "DomeCameraGun.png"; Text = "CAMERA"; Background = "#20242C"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "DoorbellGun.png"; Text = "DOORBELL"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "DeskFanGun.png"; Text = "FAN"; Background = "#4A5058"; Foreground = "#FFFFFF"; Accent = "#AEB8C4" },
    @{ File = "SecurityMonitorGun.png"; Text = "SECURITY"; Background = "#20242C"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "HighTableGun.png"; Text = "TABLE"; Background = "#5B3A22"; Foreground = "#FFF2D8"; Accent = "#C9A85A" },
    @{ File = "WorkbenchGun.png"; Text = "WORKBENCH"; Background = "#2A3038"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "PhoneCaseGun.png"; Text = "PHONE CASE"; Background = "#3B2D5C"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "WalletGun.png"; Text = "WALLET"; Background = "#3A2A1E"; Foreground = "#FFF2D8"; Accent = "#C9A85A" },
    @{ File = "ScreenProtectorGun.png"; Text = "SCREEN GUARD"; Background = "#20242C"; Foreground = "#FFFFFF"; Accent = "#8ED8FF" },
    @{ File = "FridgeGun.png"; Text = "FRIDGE"; Background = "#DDEAF2"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "DryerGun.png"; Text = "DRYER"; Background = "#D7E2EA"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "CoffeeMachineGun.png"; Text = "COFFEE"; Background = "#3B2A1E"; Foreground = "#FFF0D8"; Accent = "#C78944" },
    @{ File = "DeskChairGun.png"; Text = "CHAIR"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "PalletGun.png"; Text = "PALLET"; Background = "#7A5A32"; Foreground = "#FFF2D8"; Accent = "#3A2A16" },
    @{ File = "TrashBinGun.png"; Text = "TRASH"; Background = "#2A3038"; Foreground = "#FFFFFF"; Accent = "#7A8794" },
    @{ File = "BathroomSinkGun.png"; Text = "SINK"; Background = "#E3E9EF"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "ToiletGun.png"; Text = "TOILET"; Background = "#E3E9EF"; Foreground = "#1E3550"; Accent = "#4BB9FF" },
    @{ File = "WhiteboardGun.png"; Text = "WHITEBOARD"; Background = "#F2F4F6"; Foreground = "#1E3550"; Accent = "#8ED8FF" },
    @{ File = "VendingMachineGun.png"; Text = "VENDING"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E" },
    @{ File = "PowerStripGun.png"; Text = "POWER"; Background = "#F2F4F6"; Foreground = "#1E3550"; Accent = "#FFD34E" },
    @{ File = "CardboardBoxSmallGun.png"; Text = "BOX"; Background = "#C78944"; Foreground = "#2A1A0E"; Accent = "#FFD34E" },
    @{ File = "CardboardBoxMediumGun.png"; Text = "BOX"; Background = "#C78944"; Foreground = "#2A1A0E"; Accent = "#FFD34E" },
    @{ File = "CardboardBoxLargeGun.png"; Text = "BOX"; Background = "#C78944"; Foreground = "#2A1A0E"; Accent = "#FFD34E" },
    @{ File = "GuysquadWallSignGun.png"; Text = "GUYSQUAD"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 1024; Height = 340; TitleSize = 88 },
    @{ File = "GuysquadHangingSignGun.png"; Text = "GUYSQUAD"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 1024; Height = 340; TitleSize = 88 },
    @{ File = "HangingMobileSignGun.png"; Text = "MOBILE"; Background = "#0D4F9E"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 1024; Height = 340; TitleSize = 88 },
    @{ File = "HangingComputersSignGun.png"; Text = "COMPUTERS"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 1024; Height = 340; TitleSize = 78 },
    @{ File = "HangingAppliancesSignGun.png"; Text = "APPLIANCES"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 1024; Height = 340; TitleSize = 78 },
    @{ File = "HangingHomeTheatreSignGun.png"; Text = "HOME THEATRE"; Background = "#101820"; Foreground = "#FFFFFF"; Accent = "#4BB9FF"; Width = 1024; Height = 340; TitleSize = 72 },
    @{ File = "HangingCheckoutSignGun.png"; Text = "CHECKOUT"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 1024; Height = 340; TitleSize = 82 },
    @{ File = "BopBuyBannerGun.png"; Text = "BOPBUY"; Background = "#0D4F9E"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 1024; Height = 340; TitleSize = 96 },
    @{ File = "WelcomeDirectorySignGun.png"; Text = "WELCOME"; Background = "#2C5A45"; Foreground = "#FFF2D8"; Accent = "#C9A85A"; Width = 768; Height = 336; TitleSize = 76 },
    @{ File = "NoBreaksSignGun.png"; Text = "NO BREAKS"; Background = "#7A1717"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 512; Height = 768; TitleSize = 58 },
    @{ File = "BehindYouSignGun.png"; Text = "BEHIND YOU"; Background = "#2A1A0E"; Foreground = "#FFF2D8"; Accent = "#C9A85A"; Width = 1024; Height = 390; TitleSize = 78 },
    @{ File = "MenSignGun.png"; Text = "MEN"; Background = "#173E74"; Foreground = "#FFFFFF"; Accent = "#8ED8FF"; Width = 512; Height = 320; TitleSize = 92 },
    @{ File = "WomenSignGun.png"; Text = "WOMEN"; Background = "#5B2A5C"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 512; Height = 320; TitleSize = 78 },
    @{ File = "ClearanceSignGun.png"; Text = "CLEARANCE"; Background = "#A62E2E"; Foreground = "#FFF4D6"; Accent = "#E8C15C"; Width = 768; Height = 300; TitleSize = 76 },
    @{ File = "WarehouseSignGun.png"; Text = "WAREHOUSE"; Background = "#4A5058"; Foreground = "#FFFFFF"; Accent = "#FFD34E"; Width = 768; Height = 240; TitleSize = 68 }
)

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

foreach ($item in $items) {
    $width = if ($null -ne $item.Width) { [int]$item.Width } else { 512 }
    $height = if ($null -ne $item.Height) { [int]$item.Height } else { 256 }
    $titleSize = if ($null -ne $item.TitleSize) { [int]$item.TitleSize } else { 68 }
    $subtitleSize = if ($null -ne $item.SubtitleSize) { [int]$item.SubtitleSize } else { 20 }
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $background = [System.Drawing.ColorTranslator]::FromHtml($item.Background)
    $foreground = [System.Drawing.ColorTranslator]::FromHtml($item.Foreground)
    $accent = [System.Drawing.ColorTranslator]::FromHtml($item.Accent)

    $graphics.Clear($background)
    $accentBrush = New-Object System.Drawing.SolidBrush $accent
    $barBrush = New-Object System.Drawing.SolidBrush $background
    $foregroundBrush = New-Object System.Drawing.SolidBrush $foreground

    $graphics.FillRectangle($accentBrush, 0, 0, $width, 16)
    $graphics.FillRectangle($accentBrush, 0, $height - 16, $width, 16)

    $borderPen = New-Object System.Drawing.Pen $accent, 5
    $graphics.DrawRectangle($borderPen, 8, 8, $width - 17, $height - 17)

    $titleFont = New-Object System.Drawing.Font("Arial", $titleSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $subtitleFont = New-Object System.Drawing.Font("Arial", $subtitleSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $center = New-Object System.Drawing.StringFormat
    $center.Alignment = [System.Drawing.StringAlignment]::Center
    $center.LineAlignment = [System.Drawing.StringAlignment]::Center

    $titleHeight = [Math]::Max(80, $height - 106)
    $titleRect = New-Object System.Drawing.RectangleF 30, 30, ($width - 60), $titleHeight
    $subtitleRect = New-Object System.Drawing.RectangleF 30, ($height - 58), ($width - 60), 30
    $graphics.DrawString($item.Text, $titleFont, $foregroundBrush, $titleRect, $center)
    $graphics.DrawString("GUNVERSE", $subtitleFont, $foregroundBrush, $subtitleRect, $center)

    $titleFont.Dispose()
    $subtitleFont.Dispose()
    $center.Dispose()
    $borderPen.Dispose()
    $accentBrush.Dispose()
    $barBrush.Dispose()
    $foregroundBrush.Dispose()
    $graphics.Dispose()

    $path = Join-Path $OutputDirectory $item.File
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Output $path
}
