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
        bat 'msbuild /p:Configuration=Release'
		bat '7z a LoL-Auto-Login.zip "./LoL Auto Login/bin/x86/Release/*.dll" "./LoL Auto Login/bin/x86/Release/*.exe"'
        archiveArtifacts 'LoL-Auto-Login.zip'
        cleanWs(cleanWhenAborted: true, cleanWhenFailure: true, cleanWhenNotBuilt: true, cleanWhenSuccess: true, cleanWhenUnstable: true)
      }
    }
  }
}