pipeline {
  agent {
    node {
      label 'windows && vs-15'
    }

  }
  stages {
    stage('Build') {
      steps {
        bat 'nuget restore'
        bat 'python update_build_number.py'
        bat 'msbuild /p:Configuration=Release /p:Platform="Any CPU"'
        bat 'mkdir publish'
        bat '7z a publish/LoLAutoLogin-%GIT_BRANCH%-%BUILD_NUMBER%.zip -r "./LoL Auto Login/bin/Release/*.dll" "./LoL Auto Login/bin/Release/*.exe" "./LoL Auto Login/bin/Release/*.yaml"'
        archiveArtifacts 'publish/LoLAutoLogin-*.zip'
        bat 'iscc installer/installer.iss'
        bat '7z a publish/LoLAutoLogin-%GIT_BRANCH%-%BUILD_NUMBER%-setup.zip "./publish/*.exe"'
        archiveArtifacts 'publish/LoLAutoLogin-*-setup.zip'
      }
    }
  }
}
