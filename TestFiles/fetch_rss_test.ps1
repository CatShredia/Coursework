# Запрос RSS-фида Habr и сохранение в файл
$url    = "https://habr.com/ru/rss/articles/"
$output = "habr_rss_sample.xml"

Write-Host "Запрашиваю $url ..."

Invoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing

Write-Host "Сохранено в $output"
Write-Host ""

# Читаем и выводим первые 3000 символов для проверки
$content = Get-Content $output -Raw
Write-Host "--- Первые 3000 символов ---"
Write-Host $content.Substring(0, [Math]::Min(3000, $content.Length))
