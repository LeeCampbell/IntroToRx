#/bin/sh
# To be run inside the container
pandoc -o /output/intro-to-rx.epub metadata.md -s \
    ./content/01_WhyRx.md \
    ./content/02_KeyTypes.md \
    ./content/03_CreatingObservableSequences.md \
    ./content/04_Part2.md \
    ./content/05_Filtering.md \
    ./content/06_Transformation.md \
    ./content/07_Aggregation.md \
    ./content/08_Partitioning.md \
    ./content/09_CombiningSequences.md \
    ./content/10_Part3.md \
    ./content/11_SchedulingAndThreading.md \
    ./content/12_Timing.md \
    ./content/13_LeavingIObservable.md \
    ./content/14_ErrorHandlingOperators.md \
    ./content/15_PublishingOperators.md \
    ./content/16_TestingRx.md \
    ./content/A_IoStreams.md \
    ./content/B_Disposables.md \
    ./content/C_UsageGuidelines.md \
    ./content/D_AlgebraicUnderpinnings.md \
    --epub-cover-image intro-rx-dotnet-book-cover.png \
    --toc --toc-depth 4 \
    --resource-path "./content/"

pandoc -o /output/intro-to-rx.docx -s \
    ./content/01_WhyRx.md \
    ./content/02_KeyTypes.md \
    ./content/03_CreatingObservableSequences.md \
    ./content/04_Part2.md \
    ./content/05_Filtering.md \
    ./content/06_Transformation.md \
    ./content/07_Aggregation.md \
    ./content/08_Partitioning.md \
    ./content/09_CombiningSequences.md \
    ./content/10_Part3.md \
    ./content/11_SchedulingAndThreading.md \
    ./content/12_Timing.md \
    ./content/13_LeavingIObservable.md \
    ./content/14_ErrorHandlingOperators.md \
    ./content/15_PublishingOperators.md \
    ./content/16_TestingRx.md \
    ./content/A_IoStreams.md \
    ./content/B_Disposables.md \
    ./content/C_UsageGuidelines.md \
    ./content/D_AlgebraicUnderpinnings.md \
    --resource-path "./content/" \
    --metadata-file=metadata.md \
    --metadata title="Introduction to Rx .NET" \
    --toc --toc-depth 4

pandoc -o /output/intro-to-rx.pdf metadata.md -s \
    ./content/01_WhyRx.md \
    ./content/02_KeyTypes.md \
    ./content/03_CreatingObservableSequences.md \
    ./content/04_Part2.md \
    ./content/05_Filtering.md \
    ./content/06_Transformation.md \
    ./content/07_Aggregation.md \
    ./content/08_Partitioning.md \
    ./content/09_CombiningSequences.md \
    ./content/10_Part3.md \
    ./content/11_SchedulingAndThreading.md \
    ./content/12_Timing.md \
    ./content/13_LeavingIObservable.md \
    ./content/14_ErrorHandlingOperators.md \
    ./content/15_PublishingOperators.md \
    ./content/16_TestingRx.md \
    ./content/A_IoStreams.md \
    ./content/B_Disposables.md \
    ./content/C_UsageGuidelines.md \
    ./content/D_AlgebraicUnderpinnings.md \
    --resource-path "./content/" \
    --template ./templates/eisvogel \
    --toc --toc-depth 4

if [ -f /root/.miktex/texmfs/data/miktex/log/pdflatex.log ]; then
    cp /root/.miktex/texmfs/data/miktex/log/pdflatex.log /output
fi