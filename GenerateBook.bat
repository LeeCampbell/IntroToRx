@ECHO OFF
SET BOOKNAME=IntroToRx
ECHO "Book name is %BOOKNAME%"

ECHO Generating formatted book
REM .\KindleGenerator\KindleGenerator\bin\Debug\KindleGenerator.exe "%~dp0" "%BOOKNAME%"
.\KindleGenerator\KindleGenerator\bin\Debug\KindleGenerator.exe -f MOBI -s "%~dp0\Content" -o "%~dp0\bin\Content" -n "%BOOKNAME%" -t "Introduction to Rx" -u "An introduction to the Microsoft's Reactive Extensions (Rx)." -a "Lee Campbell" -p "Amazon.com"
IF ERRORLEVEL   1 GOTO FAIL

ECHO Generating formatted web content
.\KindleGenerator\KindleGenerator\bin\Debug\KindleGenerator.exe -f WebSite -s "%~dp0\Content" -o "%~dp0\WebSite\content\v1.0.10621.0" 
IF ERRORLEVEL   1 GOTO FAIL

ECHO Deleting cached version from local Kindle Content
pushd "%USERPROFILE%\Documents\My Kindle Content"
	IF ERRORLEVEL   1 GOTO FAILUpDir
	del /A:H "%BOOKNAME%.mbp"
	del "%BOOKNAME%.mobi"
popd

ECHO Generating Kindle book (.mobi format)
pushd %~dp0
  pushd bin\content
	REM ..\..\Tools\kindlegen.exe "%BOOKNAME%.opf" -o "%BOOKNAME%.mobi"

	REM ZIP contents folder to Calibre formats properly?
	REM COMMAND /C ..\..\GenerateBookCalibre.bat "%BOOKNAME%.opf" "%BOOKNAME%.mobi" 
	REM COMMAND /C "C:\Program Files\Calibre2\ebook-convert.exe" "%BOOKNAME%.opf" "%BOOKNAME%.mobi" --output-profile kindle --author-sort "Lee Campbell" --authors "Lee Campbell" --cover "%CD%\GraphicsIntro\Cover.jpg" --title "Introduction to Rx" --title-sort "Introduction to Rx" --chapter "/" --chapter-mark none --disable-remove-fake-margins --max-toc-links 0
	CALL "C:\Program Files\Calibre2\ebook-convert.exe" "%BOOKNAME%.opf" "%BOOKNAME%.mobi" --output-profile kindle --author-sort "Lee Campbell" --authors "Lee Campbell" --cover "%CD%\GraphicsIntro\Cover.jpg" --title "Introduction to Rx" --title-sort "Introduction to Rx" --chapter "/" --chapter-mark none --disable-remove-fake-margins --max-toc-links 0

	IF ERRORLEVEL   1 GOTO FAILUp2Dir
	move ".\%BOOKNAME%.mobi" "..\..\%BOOKNAME%.mobi"
  popd

  copy ".\%BOOKNAME%.mobi" "WebSite\content\v1.0.10621.0\%BOOKNAME%.mobi"
  ECHO Opening generated Kindle book : %BOOKNAME%.mobi
  REM	start %BOOKNAME%.mobi
  REM   START WebSite\content\v1.0.10621.0\15_SchedulingAndThreading.html
  CALL "c:\users\lee\appdata\Local\Amazon\Kindle Previewer\KindlePreviewer.exe" "c:\users\lee\Documents\ArtemisWest\Marketing\Books\Introduction To Rx\IntroToRx.mobi"
popd

time/t
pause
exit /b

:FAILUp2Dir
popd
:FAILUpDir
popd
:FAIL
ECHO Failure to generate and open book.
pause
exit /b