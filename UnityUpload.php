<?php
$myFile = $_FILES["post"]["tmp_name"];
$file_path="ScreenShot/";
$strr=$_REQUEST['Name'].".png";
move_uploaded_file($myFile,$file_path.$strr);

echo "succes ";
?>
