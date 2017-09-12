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
      }
    }
  }
}