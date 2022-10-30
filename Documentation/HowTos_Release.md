# Steps to prepare a release

1. Check that AssemblyVersion and AssemblyFileVersion have been updated correctly.
2. Rebuild the solution in the Release mode.
3. Zip 
    - ChessForge.exe
    - ChessPosition.dll 
    - EngineService.dll
    - README.MD
    - LICENSE.MD
    - Classical Sicilian.pgn
    - Stafford Gambit.pgn
    - Ridiculous Sacrifices.pgn
    - Stockfish_15_x64_avx2.exe  
    
    into __ChessForgeFull.zip__.  
 4. Zip the above, __excluding__ Stockfish_15_x64_avx2.exe, into __ChessForge.zip__.
 5. Create a new tag and a new release on GitHub.
 6. Include the above 2 zip files and remove other files added there by github.
 7. Import the release to SourceForge.org.  
