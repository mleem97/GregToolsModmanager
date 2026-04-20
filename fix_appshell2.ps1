$content = Get-Content .\AppShell.xaml -Raw; $content = $content -replace "StaticResource", "DynamicResource"; Set-Content .\AppShell.xaml -Value $content
